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
    private CoroutineHandle _flickerHandle;
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

        if (_flickerHandle.IsValid)
            Timing.KillCoroutines(_flickerHandle);

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
        int longBlackoutMinSeconds = Math.Max(1, _config.LongBlackoutMinSeconds);
        int longBlackoutMaxSeconds = Math.Max(longBlackoutMinSeconds, _config.LongBlackoutMaxSeconds);
        int poweredSeconds = Math.Max(1, _config.NormalPoweredSeconds);

        while (IsRunning)
        {
            int blackoutSeconds = _random.Next(longBlackoutMinSeconds, longBlackoutMaxSeconds + 1);
            Map.TurnOffLights();
            _lightsOn = false;

            if (_config.EnableFlickering && _random.NextDouble() <= Clamp(_config.BlackoutFlickerChance, 0f, 1f))
            {
                if (_flickerHandle.IsValid)
                    Timing.KillCoroutines(_flickerHandle);

                _flickerHandle = Timing.RunCoroutine(BlackoutFlickerRoutine(blackoutSeconds));
                _scheduledActions.Add(_flickerHandle);
            }

            yield return Timing.WaitForSeconds(blackoutSeconds);

            if (!IsRunning)
                yield break;

            Map.TurnOnLights();
            _lightsOn = true;

            if (_config.EnableFlickering && _random.NextDouble() <= Clamp(_config.PoweredFlickerChance, 0f, 1f))
            {
                if (_flickerHandle.IsValid)
                    Timing.KillCoroutines(_flickerHandle);

                _flickerHandle = Timing.RunCoroutine(SubtlePowerFlickerRoutine(poweredSeconds));
                _scheduledActions.Add(_flickerHandle);
            }

            yield return Timing.WaitForSeconds(poweredSeconds);
        }

        if (IsRunning)
        {
            Map.TurnOnLights();
            _lightsOn = true;
        }
    }

    private IEnumerator<float> BlackoutFlickerRoutine(float blackoutSeconds)
    {
        float flickerDuration = Math.Max(0.05f, _config.BlackoutFlickerDurationSeconds);
        float minInterval = Math.Max(1f, _config.BlackoutFlickerMinIntervalSeconds);
        float maxInterval = Math.Max(minInterval, _config.BlackoutFlickerMaxIntervalSeconds);

        int flickerCount = 1 + _random.Next(0, 3);
        float elapsed = 0f;

        for (int i = 0; i < flickerCount && IsRunning; i++)
        {
            float delay = (float)_random.NextDouble() * (maxInterval - minInterval) + minInterval;
            if (elapsed + delay > blackoutSeconds)
                delay = Math.Max(0.1f, blackoutSeconds - elapsed);

            yield return Timing.WaitForSeconds(delay);
            elapsed += delay;

            if (!IsRunning)
                yield break;

            if (_lightsOn)
            {
                Map.TurnOffLights();
                _lightsOn = false;
            }

            yield return Timing.WaitForSeconds(flickerDuration);

            if (!IsRunning)
                yield break;

            Map.TurnOnLights();
            _lightsOn = true;
        }
    }

    private IEnumerator<float> SubtlePowerFlickerRoutine(float poweredSeconds)
    {
        float flickerDuration = Math.Max(0.05f, _config.SubtleFlickerDurationSeconds);
        float interval = GetRandomSubtleInterval();

        if (!IsRunning)
            yield break;

        yield return Timing.WaitForSeconds(Math.Min(interval, poweredSeconds));

        if (!IsRunning || !_lightsOn || !_config.EnableFlickering)
            yield break;

        Map.TurnOffLights();
        _lightsOn = false;
        yield return Timing.WaitForSeconds(flickerDuration);

        if (!IsRunning)
            yield break;

        Map.TurnOnLights();
        _lightsOn = true;
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
        if (_flickerHandle.IsValid)
            Timing.KillCoroutines(_flickerHandle);

        _flickerHandle = default;

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

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
            return min;

        if (value > max)
            return max;

        return value;
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