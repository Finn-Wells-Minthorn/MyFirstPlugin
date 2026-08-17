using System;
using System.Threading;
using System.Timers;
using LabApi.Features.Wrappers;
using MyFirstPlugin.Config;

namespace MyFirstPlugin.Events;

public class BlackoutEvent : EventBase
{
    private const int SequenceTickMilliseconds = 1000;

    private readonly BlackoutEventConfig _config;
    private readonly System.Timers.Timer _sequenceTimer;
    private readonly System.Timers.Timer _loopTimer;
    private int _elapsedSeconds;
    private bool _lightsOn;
    private bool _looping;

    public BlackoutEvent(BlackoutEventConfig? config)
    {
        _config = config ?? new BlackoutEventConfig();

        _sequenceTimer = new System.Timers.Timer(SequenceTickMilliseconds);
        _sequenceTimer.AutoReset = true;
        _sequenceTimer.Elapsed += OnSequenceTick;

        _loopTimer = new System.Timers.Timer(GetSafeLoopIntervalMilliseconds(_config));
        _loopTimer.AutoReset = true;
        _loopTimer.Elapsed += OnLoopTick;

        TrackTimer(_sequenceTimer);
        TrackTimer(_loopTimer);
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
        _sequenceTimer.Start();
    }

    protected override void OnStop()
    {
        _sequenceTimer.Stop();
        _loopTimer.Stop();
        _looping = false;
        Map.TurnOnLights();

        Server.SendBroadcast(
            _config.EndAnnouncement,
            _config.EndAnnouncementDurationSeconds
        );
    }

    private void OnSequenceTick(object sender, ElapsedEventArgs e)
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
            _sequenceTimer.Stop();
            Map.TurnOffLights();
            _lightsOn = false;

            if (_config.EnableFlickering)
            {
                _looping = true;
                _loopTimer.Start();
            }
        }
    }

    private void OnLoopTick(object sender, ElapsedEventArgs e)
    {
        ToggleLights();
    }

    private void DoFlickerBurst(int burstCount)
    {
        int stepDurationMilliseconds = Math.Max(50, _config.FlickerStepDurationMilliseconds);

        for (int i = 0; i < burstCount; i++)
        {
            ToggleLights();
            Thread.Sleep(stepDurationMilliseconds);
            ToggleLights();
            Thread.Sleep(stepDurationMilliseconds);
        }
    }

    private static int GetSafeLoopIntervalMilliseconds(BlackoutEventConfig config)
    {
        int requested = config.FlickerStepDurationMilliseconds * 8;
        return Math.Max(1000, requested);
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