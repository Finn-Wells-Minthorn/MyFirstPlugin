using System;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public class TimeToGambleEvent : EventBase
{
    private int _affectedPlayerCount;

    public override string Name => "Time To Gamble";

    public override string Description =>
        "A modular event that strips starting equipment from human players to create a gambling-focused round state.";

    protected override void OnStart()
    {
        Server.SendBroadcast(
            "<color=orange><b>TIME TO GAMBLE</b></color>",
            10
        );

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
            $"[TimeToGambleEvent] Removed starting inventory for {affected} human players."
        );
    }

    protected override void OnStop()
    {
        Cleanup();
        _affectedPlayerCount = 0;

        Console.WriteLine("[TimeToGambleEvent] Stopped.");
    }
}
