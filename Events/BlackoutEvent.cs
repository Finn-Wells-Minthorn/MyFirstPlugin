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
                string.Empty,
                true,
                0f,
                1f
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
        while (IsRunning)
        {
            int blackoutSeconds = GetRandomBlackoutDurationSeconds();
            int poweredSeconds = Math.Max(1, _config.NormalPoweredSeconds);

            Map.TurnOffLights();
            _lightsOn = false;

            if (_config.EnableFlickering)
            {
                if (_random.NextDouble() <= Clamp(_config.BlackoutFlickerChance, 0f, 1f))
                {
                    int flickerCount = GetRandomBlackoutFlickerCount();
                    if (_flickerHandle.IsValid)
                        Timing.KillCoroutines(_flickerHandle);

                    _flickerHandle = Timing.RunCoroutine(BlackoutFlickerRoutine(blackoutSeconds, flickerCount));
                    _scheduledActions.Add(_flickerHandle);
                }
            }

            yield return Timing.WaitForSeconds(blackoutSeconds);

            if (!IsRunning)
                yield break;

            Map.TurnOnLights();
            _lightsOn = true;

            if (_config.EnableFlickering)
            {
                if (_random.NextDouble() <= Clamp(_config.PoweredFlickerChance, 0f, 1f))
                {
                    int flickerCount = GetRandomPoweredFlickerCount();
                    if (flickerCount > 0)
                    {
                        if (_flickerHandle.IsValid)
                            Timing.KillCoroutines(_flickerHandle);

                        _flickerHandle = Timing.RunCoroutine(PoweredFlickerRoutine(poweredSeconds, flickerCount));
                        _scheduledActions.Add(_flickerHandle);
                    }
                }
            }

            yield return Timing.WaitForSeconds(poweredSeconds);
        }

        if (IsRunning)
        {
            Map.TurnOnLights();
            _lightsOn = true;
        }
    }

    private int GetRandomBlackoutDurationSeconds()
    {
        int shortMin = Math.Max(1, _config.ShortBlackoutMinSeconds);
        int shortMax = Math.Max(shortMin, _config.ShortBlackoutMaxSeconds);
        int longMin = Math.Max(1, _config.LongBlackoutMinSeconds);
        int longMax = Math.Max(longMin, _config.LongBlackoutMaxSeconds);

        if (_random.NextDouble() <= Clamp(_config.ShortBlackoutChance, 0f, 1f))
            return _random.Next(shortMin, shortMax + 1);

        return _random.Next(longMin, longMax + 1);
    }

    private int GetRandomBlackoutFlickerCount()
    {
        double roll = _random.NextDouble();
        if (roll < 0.5)
            return 0;

        if (roll < 0.85)
            return 1;

        if (roll < 0.98)
            return 2;

        return 3;
    }

    private int GetRandomPoweredFlickerCount()
    {
        double roll = _random.NextDouble();
        if (roll < 0.45)
            return 0;

        if (roll < 0.8)
            return 1;

        return 2;
    }

    private IEnumerator<float> BlackoutFlickerRoutine(float blackoutSeconds, int flickerCount)
    {
        if (flickerCount <= 0 || blackoutSeconds <= 0f)
            yield break;

        float flickerDuration = Math.Max(0.05f, _config.BlackoutFlickerDurationSeconds);
        float minTime = Math.Max(1f, blackoutSeconds * 0.1f);
        float maxTime = Math.Max(minTime + 0.5f, blackoutSeconds - minTime - flickerDuration);

        List<float> times = new();
        float minimumGap = Math.Max(2f, blackoutSeconds / 12f);

        for (int i = 0; i < flickerCount && IsRunning; i++)
        {
            float candidate;
            int attempts = 0;
            do
            {
                candidate = (float)_random.NextDouble() * (maxTime - minTime) + minTime;
                attempts++;
            }
            while (attempts < 20 && times.Exists(existing => Math.Abs(candidate - existing) < minimumGap));

            if (candidate <= minTime || candidate >= blackoutSeconds - flickerDuration)
                continue;

            times.Add(candidate);
        }

        if (times.Count == 0)
            yield break;

        times.Sort();
        float elapsed = 0f;

        foreach (float when in times)
        {
            if (!IsRunning)
                yield break;

            float delay = when - elapsed;
            if (delay > 0f)
                yield return Timing.WaitForSeconds(delay);

            elapsed = when;

            if (!IsRunning || !_lightsOn)
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

    private IEnumerator<float> PoweredFlickerRoutine(float poweredSeconds, int flickerCount)
    {
        if (flickerCount <= 0 || poweredSeconds <= 0f)
            yield break;

        float flickerDuration = Math.Max(0.05f, _config.SubtleFlickerDurationSeconds);
        float minTime = Math.Max(1f, poweredSeconds * 0.12f);
        float maxTime = Math.Max(minTime + 0.5f, poweredSeconds - 1f - flickerDuration);

        List<float> times = new();
        float minimumGap = Math.Max(2f, poweredSeconds / 8f);

        for (int i = 0; i < flickerCount && IsRunning; i++)
        {
            float candidate;
            int attempts = 0;
            do
            {
                candidate = (float)_random.NextDouble() * (maxTime - minTime) + minTime;
                attempts++;
            }
            while (attempts < 20 && times.Exists(existing => Math.Abs(candidate - existing) < minimumGap));

            if (candidate <= minTime || candidate >= poweredSeconds - flickerDuration)
                continue;

            times.Add(candidate);
        }

        if (times.Count == 0)
            yield break;

        times.Sort();
        float elapsed = 0f;

        foreach (float when in times)
        {
            if (!IsRunning || !_lightsOn)
                yield break;

            float delay = when - elapsed;
            if (delay > 0f)
                yield return Timing.WaitForSeconds(delay);

            elapsed = when;

            if (!IsRunning || !_lightsOn)
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