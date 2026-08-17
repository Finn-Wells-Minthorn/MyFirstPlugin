using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MEC;

namespace MyFirstPlugin.Events;

public sealed class EventRollPresenter
{
    private readonly EventRollConfig _config;
    private CoroutineHandle _rollHandle;
    private bool _isCancelled;
    private bool _isRunning;

    public EventRollPresenter()
        : this(new EventRollConfig())
    {
    }

    public EventRollPresenter(EventRollConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public bool IsRunning => _isRunning && _rollHandle.IsValid;

    public void Start(EventBase selectedEvent, IReadOnlyList<EventBase> enabledEvents, Action<EventBase>? onCompleted)
    {
        if (selectedEvent == null)
            throw new ArgumentNullException(nameof(selectedEvent));

        if (enabledEvents == null)
            throw new ArgumentNullException(nameof(enabledEvents));

        Cancel();

        List<string> eventNames = enabledEvents
            .Where(x => x != null && x.IsEnabled && !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (eventNames.Count == 0)
        {
            _isRunning = false;
            onCompleted?.Invoke(selectedEvent);
            return;
        }

        if (!eventNames.Contains(selectedEvent.Name, StringComparer.OrdinalIgnoreCase))
        {
            eventNames.Add(selectedEvent.Name);
        }

        _isCancelled = false;
        _isRunning = true;
        _rollHandle = Timing.RunCoroutine(RunRoll(selectedEvent, eventNames, onCompleted));
    }

    public void Cancel()
    {
        _isCancelled = true;

        if (_rollHandle.IsValid)
            Timing.KillCoroutines(_rollHandle);

        _rollHandle = default;
        _isRunning = false;
    }

    private IEnumerator<float> RunRoll(EventBase selectedEvent, List<string> eventNames, Action<EventBase>? onCompleted)
    {
        try
        {
            if (_isCancelled)
                yield break;

            Server.SendBroadcast("EVENT SELECTING...", 1);
            yield return Timing.WaitForSeconds(0.35f);

            int winnerIndex = eventNames.FindIndex(x => string.Equals(x, selectedEvent.Name, StringComparison.OrdinalIgnoreCase));
            if (winnerIndex < 0)
                winnerIndex = 0;

            int currentIndex = 0;
            float interval = Math.Max(0.08f, _config.InitialIntervalSeconds);
            int stepCount = Math.Max(18, _config.RollIterationCount);

            for (int i = 0; i < stepCount && !_isCancelled; i++)
            {
                if (i >= Math.Max(5, stepCount - 6))
                {
                    currentIndex = winnerIndex;
                }
                else
                {
                    currentIndex = (currentIndex + 1) % eventNames.Count;
                }

                Server.SendBroadcast(eventNames[currentIndex], 1);
                yield return Timing.WaitForSeconds(interval);

                if (i < 12)
                {
                    interval = Math.Min(_config.MaxIntervalSeconds, interval + 0.06f);
                }
                else
                {
                    interval = Math.Min(_config.MaxIntervalSeconds, interval + 0.12f);
                }
            }

            if (_isCancelled)
                yield break;

            Server.SendBroadcast("EVENT SELECTED", 2);
            yield return Timing.WaitForSeconds(0.5f);

            if (_isCancelled)
                yield break;

            Server.SendBroadcast(selectedEvent.Name, _config.FinalResultDisplaySeconds);
            yield return Timing.WaitForSeconds(Math.Max(0.25f, _config.FinalResultDisplaySeconds / 3f));

            if (_isCancelled)
                yield break;

            onCompleted?.Invoke(selectedEvent);
        }
        finally
        {
            _isRunning = false;
            _rollHandle = default;
            _isCancelled = false;
        }
    }
}

public class EventRollConfig
{
    public float InitialIntervalSeconds { get; set; } = 0.12f;

    public float MaxIntervalSeconds { get; set; } = 0.9f;

    public ushort FinalResultDisplaySeconds { get; set; } = 3;

    public int RollIterationCount { get; set; } = 22;
}
