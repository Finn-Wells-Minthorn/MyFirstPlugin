using System;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace MyFirstPlugin.Events;

public class TimeToGambleEvent : EventBase
{
    private readonly TimeToGambleMachineManager _machineManager = new();
    private int _affectedPlayerCount;

    public override string Name => "Time To Gamble";

    public override string Description =>
        "A modular event that strips starting equipment from human players and exposes gambling-machine interaction checks.";

    protected override void OnStart()
    {
        Server.SendBroadcast(
            "<color=orange><b>TIME TO GAMBLE</b></color>",
            10
        );

        _machineManager.Clear();
        _machineManager.RegisterMachine(new GamblingMachine("mtf-gamble-machine", Vector3.zero, GamblingMachineTeamType.Mtf, 5f));
        _machineManager.RegisterMachine(new GamblingMachine("scientist-gamble-machine", Vector3.zero, GamblingMachineTeamType.Scientist, 5f));
        _machineManager.Subscribe();

        int affected = 0;

        foreach (Player player in Player.List)
        {
            if (player == null || !player.IsHuman || !player.IsAlive)
                continue;

            player.ClearInventory(true, true);
            affected++;
        }

        _affectedPlayerCount = affected;

        Console.WriteLine(
            $"[TimeToGambleEvent] Removed starting inventory for {affected} human players and enabled machine checks."
        );
    }

    protected override void OnStop()
    {
        _machineManager.Unsubscribe();
        _machineManager.Clear();
        Cleanup();
        _affectedPlayerCount = 0;

        Console.WriteLine("[TimeToGambleEvent] Stopped.");
    }
}
