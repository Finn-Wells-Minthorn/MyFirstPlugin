using System;
using System.Timers;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    private readonly Timer _flickerTimer;
    private readonly Timer _powerFailureTimer;
    private bool _lightsOn;
    private int _powerFailurePhase;

    public TestEvent()
    {
        _flickerTimer = new Timer(2500);
        _flickerTimer.AutoReset = true;
        _flickerTimer.Elapsed += OnFlickerTick;

        _powerFailureTimer = new Timer(5000);
        _powerFailureTimer.AutoReset = false;
        _powerFailureTimer.Elapsed += (_, _) => StartFullBlackout();
    }

    public override string Name => "Blackout Event";

    public override string Description =>
        "A cinematic blackout that flickers for a moment before the power fully fails and the lights settle into a darker rhythm.";

    public override void Start()
    {
        Server.SendBroadcast(
            "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>",
            10
        );

        _lightsOn = true;
        _powerFailurePhase = 0;
        Map.TurnOnLights();
        _flickerTimer.Start();
        _powerFailureTimer.Start();
    }

    public override void Stop()
    {
        _flickerTimer.Stop();
        _powerFailureTimer.Stop();
        Map.TurnOnLights();

        Server.SendBroadcast(
            "<color=green><b>Power restored. The blackout has ended.</b></color>",
            5
        );
    }

    private void OnFlickerTick(object sender, ElapsedEventArgs e)
    {
        if (_powerFailurePhase < 2)
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

    private void StartFullBlackout()
    {
        _powerFailurePhase = 2;
        _flickerTimer.Interval = 2500;

        Server.SendBroadcast(
            "<color=red><b>Power failure! The lights are going out.</b></color>",
            5
        );

        Map.TurnOffLights();
        _lightsOn = false;
    }
}