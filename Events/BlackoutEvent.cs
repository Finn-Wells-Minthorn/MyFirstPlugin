using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using MEC;
using MyFirstPlugin.Config;

namespace MyFirstPlugin.Events;

public class BlackoutEvent : EventBase
{
    private const int SequenceTickSeconds = 1;

    private readonly BlackoutEventConfig _config;
    private readonly List<CoroutineHandle> _scheduledActions = new();
    private CoroutineHandle _sequenceHandle;
    private CoroutineHandle _loopHandle;
    private int _elapsedSeconds;
    private bool _lightsOn;
    private bool _looping;

    public BlackoutEvent(BlackoutEventConfig? config)
    {
        _config = config ?? new BlackoutEventConfig();
    }

    public override string Name => "Blackout Event";

    public override string Description =>
        "A cinematic blackout sequence with staged flickers before the full outage, then a calmer looping flicker to keep players able to see.";

    protected override void OnStart()
    {
        Server.SendBroadcast(
            _config.StartAnnouncement,
            _config.StartAnnouncementDurationSeconds
        );

        _elapsedSeconds = 0;
        _lightsOn = true;
        _looping = false;
        Map.TurnOnLights();

        _sequenceHandle = Timing.CallContinuously(
            SequenceTickSeconds,
            OnSequenceTick,
            () => { }
        );
    }

    protected override void OnStop()
    {
        CancelScheduledHandles();

        if (_sequenceHandle.IsValid)
            Timing.KillCoroutines(_sequenceHandle);

        if (_loopHandle.IsValid)
            Timing.KillCoroutines(_loopHandle);

        _looping = false;
        _lightsOn = true;
        Map.TurnOnLights();

        Server.SendBroadcast(
            _config.EndAnnouncement,
            _config.EndAnnouncementDurationSeconds
        );
    }

    private void OnSequenceTick()
    {
        int blackoutDurationSeconds = Math.Max(1, _config.BlackoutDurationSeconds);
        if (_elapsedSeconds >= blackoutDurationSeconds)
        {
            EventManager.StopCurrentEvent();
            return;
        }

        if (_looping)
            return;

        _elapsedSeconds++;

        if (_config.EnableFlickering && _elapsedSeconds == 1)
        {
            DoFlickerBurst(1);
            return;
        }

        if (_config.EnableFlickering && _elapsedSeconds == 21)
        {
            DoFlickerBurst(2);
            return;
        }

        if (_config.EnableFlickering && _elapsedSeconds == 31)
        {
            DoFlickerBurst(3);
            return;
        }

        if (_config.EnableFlickering && _elapsedSeconds == 36)
        {
            DoFlickerBurst(1);
            return;
        }

        int transitionDelaySeconds = Math.Max(1, _config.FlickerTransitionDelaySeconds);
        if (_elapsedSeconds == transitionDelaySeconds)
        {
            if (_sequenceHandle.IsValid)
                Timing.KillCoroutines(_sequenceHandle);

            Map.TurnOffLights();
            _lightsOn = false;

            if (_config.EnableFlickering)
            {
                _looping = true;
                _loopHandle = Timing.CallContinuously(
                    GetSafeLoopIntervalSeconds(_config),
                    ToggleLights,
                    () => { }
                );
            }
        }
    }

    private void DoFlickerBurst(int burstCount)
    {
        int stepDurationMilliseconds = Math.Max(50, _config.FlickerStepDurationMilliseconds);
        float stepDurationSeconds = stepDurationMilliseconds / 1000f;

        for (int i = 0; i < burstCount; i++)
        {
            float delayA = i * 2f * stepDurationSeconds;
            float delayB = delayA + stepDurationSeconds;

            _scheduledActions.Add(Timing.CallDelayed(delayA, ToggleLights));
            _scheduledActions.Add(Timing.CallDelayed(delayB, ToggleLights));
        }
    }

    private void CancelScheduledHandles()
    {
        for (int i = _scheduledActions.Count - 1; i >= 0; i--)
        {
            var handle = _scheduledActions[i];
            if (handle.IsValid)
                Timing.KillCoroutines(handle);
        }

        _scheduledActions.Clear();
    }

    private static float GetSafeLoopIntervalSeconds(BlackoutEventConfig config)
    {
        int requestedMilliseconds = config.FlickerStepDurationMilliseconds * 8;
        int safeMilliseconds = Math.Max(1000, requestedMilliseconds);
        return safeMilliseconds / 1000f;
    }

    private void ToggleLights()
    {
        _lightsOn = !_lightsOn;

        if (_lightsOn)
        {
            Map.TurnOnLights();
        }
        else
        {
            Map.TurnOffLights();
        }
    }
}