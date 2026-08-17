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
    private readonly Random _random = new();
    private CoroutineHandle _sequenceHandle;
    private CoroutineHandle _normalCycleHandle;
    private int _elapsedSeconds;
    private bool _lightsOn;
    private bool _normalCycleRunning;

    public BlackoutEvent(BlackoutEventConfig? config)
    {
        _config = config ?? new BlackoutEventConfig();
    }

    public override string Name => "Blackout Event";

    public override string Description =>
        "A cinematic blackout sequence with staged flickers before the full outage, then a calmer looping flicker to keep players able to see.";

    protected override void OnStart()
    {
        if (_config.EnableCassieAnnouncement && !string.IsNullOrWhiteSpace(_config.CassieAnnouncementText))
        {
            LabApi.Features.Wrappers.Announcer.Message(
                _config.CassieAnnouncementText,
                _config.CassieAnnouncementText,
                false,
                1f,
                0f
            );
        }

        Server.SendBroadcast(
            _config.StartAnnouncement,
            _config.StartAnnouncementDurationSeconds
        );

        _elapsedSeconds = 0;
        _lightsOn = true;
        _normalCycleRunning = false;
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

        if (_normalCycleHandle.IsValid)
            Timing.KillCoroutines(_normalCycleHandle);

        _normalCycleRunning = false;
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

        if (_normalCycleRunning)
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
            _normalCycleRunning = true;
            StartNormalPostCinematicCycle();
        }
    }

    private void StartNormalPostCinematicCycle()
    {
        if (_normalCycleHandle.IsValid)
            Timing.KillCoroutines(_normalCycleHandle);

        _normalCycleHandle = Timing.RunCoroutine(NormalCycleRoutine());
        _scheduledActions.Add(_normalCycleHandle);
    }

    private IEnumerator<float> NormalCycleRoutine()
    {
        int blackoutDurationSeconds = Math.Max(1, _config.BlackoutDurationSeconds);
        int shortBlackoutSeconds = Math.Max(1, _config.NormalShortBlackoutSeconds);
        int poweredSeconds = Math.Max(1, _config.NormalPoweredSeconds);
        int longBlackoutMinSeconds = Math.Max(180, _config.LongBlackoutMinSeconds);
        int longBlackoutMaxSeconds = Math.Max(longBlackoutMinSeconds, _config.LongBlackoutMaxSeconds);

        while (IsRunning && _elapsedSeconds < blackoutDurationSeconds)
        {
            Map.TurnOffLights();
            _lightsOn = false;
            yield return Timing.WaitForSeconds(shortBlackoutSeconds);

            if (!IsRunning || _elapsedSeconds >= blackoutDurationSeconds)
                yield break;

            Map.TurnOnLights();
            _lightsOn = true;
            if (_config.EnableFlickering)
            {
                _scheduledActions.Add(Timing.RunCoroutine(SubtlePowerFlickerRoutine(poweredSeconds)));
            }

            yield return Timing.WaitForSeconds(poweredSeconds);

            if (!IsRunning || _elapsedSeconds >= blackoutDurationSeconds)
                yield break;

            Map.TurnOffLights();
            _lightsOn = false;

            int longBlackoutSeconds = _random.Next(longBlackoutMinSeconds, longBlackoutMaxSeconds + 1);
            yield return Timing.WaitForSeconds(longBlackoutSeconds);

            if (!IsRunning || _elapsedSeconds >= blackoutDurationSeconds)
                yield break;

            Map.TurnOnLights();
            _lightsOn = true;
            if (_config.EnableFlickering)
            {
                _scheduledActions.Add(Timing.RunCoroutine(SubtlePowerFlickerRoutine(poweredSeconds)));
            }

            yield return Timing.WaitForSeconds(poweredSeconds);
        }

        if (IsRunning)
        {
            Map.TurnOnLights();
            _lightsOn = true;
        }
    }

    private IEnumerator<float> SubtlePowerFlickerRoutine(float poweredSeconds)
    {
        float elapsed = 0f;
        while (IsRunning && elapsed < poweredSeconds)
        {
            float interval = GetRandomSubtleInterval();
            if (elapsed + interval > poweredSeconds)
                interval = poweredSeconds - elapsed;

            yield return Timing.WaitForSeconds(interval);
            elapsed += interval;

            if (!IsRunning || !_lightsOn || !_config.EnableFlickering)
                yield break;

            float flickerDuration = Math.Min(_config.SubtleFlickerDurationSeconds, poweredSeconds - elapsed);
            if (flickerDuration <= 0f)
                yield break;

            Map.TurnOffLights();
            _lightsOn = false;
            yield return Timing.WaitForSeconds(flickerDuration);

            if (!IsRunning)
                yield break;

            Map.TurnOnLights();
            _lightsOn = true;
        }
    }

    private float GetRandomSubtleInterval()
    {
        float min = Math.Max(1f, _config.SubtleFlickerMinIntervalSeconds);
        float max = Math.Max(min, _config.SubtleFlickerMaxIntervalSeconds);
        return (float)_random.NextDouble() * (max - min) + min;
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
            CoroutineHandle handle = _scheduledActions[i];
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