using System;
using System.Timers;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    private readonly Timer _flickerTimer;
    private bool _lightsOn;

    public TestEvent()
    {
        _flickerTimer = new Timer(450);
        _flickerTimer.AutoReset = true;
        _flickerTimer.Elapsed += OnFlickerTick;
    }

    public override string Name => "Blackout Event";

    public override string Description =>
        "A flickering blackout where the facility goes dark and everyone gets a lantern for safety.";

    public override void Start()
    {
        Server.SendBroadcast(
            "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>",
            10
        );

        foreach (Player player in Player.List)
        {
            player.AddItem(ItemType.Lantern);
        }

        _lightsOn = false;
        _flickerTimer.Start();
        Map.TurnOffLights();
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