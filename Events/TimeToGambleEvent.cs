using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MyFirstPlugin.Config;

namespace MyFirstPlugin.Events;

public class TimeToGambleEvent : EventBase
{
    private readonly TimeToGambleMachineManager _machineManager = new();
    private readonly TimeToGambleEventConfig _config;
    private int _affectedPlayerCount;

    public TimeToGambleEvent(TimeToGambleEventConfig? config = null)
    {
        _config = config ?? new TimeToGambleEventConfig();
    }

    public override string Name => "Time To Gamble";

    public override string Description =>
        "A modular event that strips starting equipment from human players and detects interaction with one existing workstation.";

    protected override void OnStart()
    {
        Server.SendBroadcast(
            "<color=orange><b>TIME TO GAMBLE</b></color>",
            10
        );

        _machineManager.Unsubscribe();
        _machineManager.Clear();

        Room? targetRoom = ResolveTargetRoom();
        if (targetRoom == null)
        {
            Console.WriteLine($"[TimeToGambleEvent] Failed to find target room '{_config.TargetRoomName}'. No gamble terminal was registered.");
            return;
        }

        Console.WriteLine($"[SCPEventSystem] Gamble target room found: name='{targetRoom.Name}', position='{targetRoom.Position}', zone='{targetRoom.Zone}'.");

        Workstation? targetWorkstation = Workstation.List
            .Where(workstation => workstation.Room != null && workstation.Room.Name == _config.TargetRoomName)
            .ElementAtOrDefault(_config.TargetWorkstationIndex);

        if (targetWorkstation == null)
        {
            Console.WriteLine($"[SCPEventSystem] No existing workstation found in room '{targetRoom.Name}' at configured index {_config.TargetWorkstationIndex}.");
            return;
        }

        Console.WriteLine($"[SCPEventSystem] Existing gamble terminal found: type='{targetWorkstation.GetType().FullName}', room='{targetWorkstation.Room?.Name}', position='{targetWorkstation.Position}'.");

        GamblingMachine gambleMachine = new GamblingMachine(
            "gamble-terminal",
            GamblingMachineTeamType.Mtf,
            5f
        );

        _machineManager.RegisterMachine(gambleMachine, targetWorkstation);
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
            $"[TimeToGambleEvent] Removed starting inventory for {affected} human players and started existing-terminal interaction detection."
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

    private Room? ResolveTargetRoom()
    {
        IEnumerable<Room> rooms = Room.Get(_config.TargetRoomName);
        Room? room = rooms.FirstOrDefault();

        if (room == null)
            room = Map.Rooms.FirstOrDefault(r => r.Name == _config.TargetRoomName);

        return room;
    }
}
