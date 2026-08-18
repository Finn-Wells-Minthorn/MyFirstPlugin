using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MyFirstPlugin.Config;
using UnityEngine;

namespace MyFirstPlugin.Events;

public class TimeToGambleEvent : EventBase
{
    private readonly TimeToGambleMachineManager _machineManager = new();
    private readonly List<InteractableToy> _spawnedMachines = new();
    private readonly TimeToGambleEventConfig _config;
    private int _affectedPlayerCount;

    public TimeToGambleEvent(TimeToGambleEventConfig? config = null)
    {
        _config = config ?? new TimeToGambleEventConfig();
    }

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
        _machineManager.Unsubscribe();
        _spawnedMachines.Clear();

        Room? targetRoom = ResolveTargetRoom();
        if (targetRoom == null)
        {
            Console.WriteLine($"[TimeToGambleEvent] Failed to find target room '{_config.TargetRoomName}'. No machines were spawned.");
            return;
        }

        Console.WriteLine($"[TimeToGambleEvent] Found target room '{targetRoom.Name}' at {targetRoom.Position} in zone '{targetRoom.Zone}'.");

        Vector3 mtfPosition = GetPositionForMachine(targetRoom, -1);
        Vector3 scientistPosition = GetPositionForMachine(targetRoom, 1);

        Console.WriteLine($"[TimeToGambleEvent] MTF machine position: {mtfPosition}");
        Console.WriteLine($"[TimeToGambleEvent] Scientist machine position: {scientistPosition}");

        GamblingMachine mtfMachine = new GamblingMachine("mtf-gamble-machine", mtfPosition, GamblingMachineTeamType.Mtf, 5f);
        GamblingMachine scientistMachine = new GamblingMachine("scientist-gamble-machine", scientistPosition, GamblingMachineTeamType.Scientist, 5f);

        InteractableToy? mtfToy = SpawnMachine(mtfMachine, _config.MtfMachineDisplayName);
        InteractableToy? scientistToy = SpawnMachine(scientistMachine, _config.ScientistMachineDisplayName);

        if (mtfToy != null)
        {
            bool mtfBound = TryBindMachine(mtfMachine, mtfToy);
            Console.WriteLine($"[TimeToGambleEvent] MTF BindToy(...) succeeded: {mtfBound}");
            if (mtfBound)
                _machineManager.RegisterMachine(mtfMachine);
        }

        if (scientistToy != null)
        {
            bool scientistBound = TryBindMachine(scientistMachine, scientistToy);
            Console.WriteLine($"[TimeToGambleEvent] Scientist BindToy(...) succeeded: {scientistBound}");
            if (scientistBound)
                _machineManager.RegisterMachine(scientistMachine);
        }

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
            $"[TimeToGambleEvent] Removed starting inventory for {affected} human players and created {_spawnedMachines.Count} gambling machines."
        );
    }

    protected override void OnStop()
    {
        _machineManager.Unsubscribe();
        _machineManager.Clear();

        foreach (InteractableToy toy in _spawnedMachines)
        {
            try
            {
                if (toy != null && !toy.IsDestroyed)
                    toy.Destroy();
            }
            catch
            {
                // Ignore cleanup errors so a single event can fail without corrupting the registry.
            }
        }

        _spawnedMachines.Clear();
        Cleanup();
        _affectedPlayerCount = 0;

        Console.WriteLine("[TimeToGambleEvent] Stopped.");
    }

    private Room? ResolveTargetRoom()
    {
        IEnumerable<Room> rooms = Room.Get(_config.TargetRoomName);
        Room? room = rooms.FirstOrDefault();

        if (room == null)
        {
            room = Map.Rooms.FirstOrDefault(r => r.Name == _config.TargetRoomName);
        }

        return room;
    }

    private Vector3 GetPositionForMachine(Room room, int side)
    {
        float xOffset = side * _config.MachineSeparationOffset;
        return new Vector3(
            room.Position.x + xOffset,
            room.Position.y + _config.MachineHeightOffset,
            room.Position.z + (side > 0 ? 1.5f : -1.5f)
        );
    }

    private InteractableToy? SpawnMachine(GamblingMachine machine, string displayName)
    {
        if (machine == null)
            throw new ArgumentNullException(nameof(machine));

        InteractableToy toy = InteractableToy.Create(
            machine.Position,
            Quaternion.identity,
            null,
            false
        );

        Console.WriteLine($"[TimeToGambleEvent] Attempted to create machine '{machine.Id}' at {machine.Position}. Result: {(toy == null ? "NULL" : "SUCCESS")}");

        if (toy == null)
        {
            Console.WriteLine($"[TimeToGambleEvent] Failed to create machine '{machine.Id}'.");
            return null;
        }

        toy.InteractionDuration = 1.5f;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            toy.GameObject.name = displayName;
        }

        _spawnedMachines.Add(toy);
        Console.WriteLine($"[TimeToGambleEvent] Spawned physical gamble toy '{machine.Id}' at {machine.Position} with display name '{displayName}'.");

        return toy;
    }

    private bool TryBindMachine(GamblingMachine machine, InteractableToy toy)
    {
        try
        {
            machine.BindToy(toy);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TimeToGambleEvent] BindToy failed for machine '{machine.Id}': {ex.Message}");
            return false;
        }
    }
}
