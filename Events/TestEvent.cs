using System;
using System.Timers;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    private readonly Timer _flickerTimer;
    private readonly Timer _lanternDelayTimer;
    private bool _lightsOn;

    public TestEvent()
    {
        _flickerTimer = new Timer(2500);
        _flickerTimer.AutoReset = true;
        _flickerTimer.Elapsed += OnFlickerTick;

        _lanternDelayTimer = new Timer(7000);
        _lanternDelayTimer.AutoReset = false;
        _lanternDelayTimer.Elapsed += (_, _) => GiveLanterns();
    }

    public override string Name => "Blackout Event";

    public override string Description =>
        "A slow, flickering blackout where the facility goes dark and lanterns are given after the round is active.";

    public override void Start()
    {
        Server.SendBroadcast(
            "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>",
            10
        );

        _lightsOn = false;
        Map.TurnOffLights();
        _flickerTimer.Start();
        _lanternDelayTimer.Start();
    }

    public override void Stop()
    {
        _flickerTimer.Stop();
        _lanternDelayTimer.Stop();
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

    private void GiveLanterns()
    {
        foreach (Player player in Player.List)
        {
            player.AddItem(ItemType.Lantern);
        }
    }
}