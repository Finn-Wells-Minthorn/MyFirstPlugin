using System;
using System.Timers;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    private readonly Timer _flickerTimer;
    private bool _lightsOn;

    public TestEvent()
    {
        _flickerTimer = new Timer(2500);
        _flickerTimer.AutoReset = true;
        _flickerTimer.Elapsed += OnFlickerTick;
    }

    public override string Name => "Blackout Event";

    public override string Description =>
        "A slow, flickering blackout that darkens the facility without the unstable item-grant behavior.";

    public override void Start()
    {
        Server.SendBroadcast(
            "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>",
            10
        );

        _lightsOn = false;
        Map.TurnOffLights();
        _flickerTimer.Start();
    }

    public override void Stop()
    {
        _flickerTimer.Stop();
        Map.TurnOnLights();

        Server.SendBroadcast(
            "<color=green><b>Power restored. The blackout has ended.</b></color>",
            5
        );
    }

    private void OnFlickerTick(object sender, ElapsedEventArgs e)
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