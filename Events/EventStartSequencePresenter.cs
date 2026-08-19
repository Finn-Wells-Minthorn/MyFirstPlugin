using System;
using System.Collections.Generic;
using MEC;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public sealed class EventStartSequencePresenter
{
    private CoroutineHandle _sequenceHandle;
    private bool _isCancelled;
    private bool _isRunning;

    public bool IsRunning => _isRunning && _sequenceHandle.IsValid;

    public void Start(Action onCompleted)
    {
        if (onCompleted == null)
            throw new ArgumentNullException(nameof(onCompleted));

        Cancel();

        _isCancelled = false;
        _isRunning = true;
        _sequenceHandle = Timing.RunCoroutine(RunSequence(onCompleted));
    }

    public void Cancel()
    {
        _isCancelled = true;

        if (_sequenceHandle.IsValid)
            Timing.KillCoroutines(_sequenceHandle);

        _sequenceHandle = default;
        _isRunning = false;
    }

    private IEnumerator<float> RunSequence(Action onCompleted)
    {
        try
        {
            if (_isCancelled)
                yield break;

            Server.SendBroadcast("SPECIAL EVENT", 2);
            yield return Timing.WaitForSeconds(0.6f);

            foreach (int count in new[] { 3, 2, 1 })
            {
                if (_isCancelled)
                    yield break;

                Server.SendBroadcast(count.ToString(), 1);
                yield return Timing.WaitForSeconds(0.6f);
            }

            if (_isCancelled)
                yield break;

            Server.SendBroadcast("EVENT SELECTING...", 2);
            yield return Timing.WaitForSeconds(0.35f);

            if (_isCancelled)
                yield break;

            onCompleted();
        }
        finally
        {
            _isCancelled = false;
            _isRunning = false;
            _sequenceHandle = default;
        }
    }
}
