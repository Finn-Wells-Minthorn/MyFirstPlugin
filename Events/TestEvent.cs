using System;
using System.Threading;
using System.Timers;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    private readonly System.Timers.Timer _sequenceTimer;
    private readonly System.Timers.Timer _loopTimer;
    private int _elapsedSeconds;
    private bool _lightsOn;
    private bool _looping;

    public TestEvent()
    {
        _sequenceTimer = new System.Timers.Timer(1000);
        _sequenceTimer.AutoReset = true;
        _sequenceTimer.Elapsed += OnSequenceTick;

        _loopTimer = new System.Timers.Timer(2500);
        _loopTimer.AutoReset = true;
        _loopTimer.Elapsed += OnLoopTick;
    }

    public override string Name => "Blackout Event";

    public override string Description =>
        "A cinematic blackout sequence with staged flickers before the full outage, then a calmer looping flicker to keep players able to see.";

    public override void Start()
    {
        Server.SendBroadcast(
            "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>",
            10
        );

        _elapsedSeconds = 0;
        _lightsOn = true;
        _looping = false;
        Map.TurnOnLights();
        _sequenceTimer.Start();
    }

    public override void Stop()
    {
        _sequenceTimer.Stop();
        _loopTimer.Stop();
        _looping = false;
        Map.TurnOnLights();

        Server.SendBroadcast(
            "<color=green><b>Power restored. The blackout has ended.</b></color>",
            5
        );
    }

    private void OnSequenceTick(object sender, ElapsedEventArgs e)
    {
        if (_looping)
            return;

        _elapsedSeconds++;

        if (_elapsedSeconds == 1)
        {
            DoFlickerBurst(1);
            return;
        }

        if (_elapsedSeconds == 21)
        {
            DoFlickerBurst(2);
            return;
        }

        if (_elapsedSeconds == 31)
        {
            DoFlickerBurst(3);
            return;
        }

        if (_elapsedSeconds == 36)
        {
            DoFlickerBurst(1);
            return;
        }

        if (_elapsedSeconds == 41)
        {
            _sequenceTimer.Stop();
            Map.TurnOffLights();
            _lightsOn = false;
            _looping = true;
            _loopTimer.Start();
        }
    }

    private void OnLoopTick(object sender, ElapsedEventArgs e)
    {
        ToggleLights();
    }

    private void DoFlickerBurst(int burstCount)
    {
        for (int i = 0; i < burstCount; i++)
        {
            ToggleLights();
            Thread.Sleep(200);
            ToggleLights();
            Thread.Sleep(250);
        }
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