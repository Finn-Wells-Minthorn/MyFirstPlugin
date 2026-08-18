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

        Console.WriteLine($"[GAMBLE DEBUG] room found -> name='{targetRoom.Name}', position='{targetRoom.Position}', zone='{targetRoom.Zone}'");

        Vector3 mtfPosition = GetPositionForMachine(targetRoom, -1);
        Vector3 scientistPosition = GetPositionForMachine(targetRoom, 1);

        Console.WriteLine($"[GAMBLE DEBUG] position calculated -> mtf='{mtfPosition}', scientist='{scientistPosition}'");

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

        Console.WriteLine($"[GAMBLE DEBUG] InteractableToy.Create -> machine='{machine.Id}', targetPosition='{machine.Position}', room='{_config.TargetRoomName}'");
        Console.WriteLine($"[GAMBLE DEBUG] InteractableToy.Create result -> toyIsNull={toy == null}");

        if (toy != null)
        {
            var rotation = (toy.Transform != null) ? toy.Transform.rotation : Quaternion.identity;
            var isActive = (toy.GameObject != null) && toy.GameObject.activeSelf;
            var isEnabled = (toy.GameObject != null) && toy.GameObject.activeInHierarchy;
            Console.WriteLine($"[GAMBLE DEBUG] toy object -> type='{toy.GetType().FullName}', instance='{toy}', position='{toy.Position}', rotation='{rotation}', active='{isActive}', enabled='{isEnabled}'");
        }

        if (toy == null)
        {
            Console.WriteLine($"[GAMBLE DEBUG] FAIL -> InteractableToy.Create returned null for '{machine.Id}'.");
            return null;
        }

        toy.InteractionDuration = 1.5f;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            toy.GameObject.name = displayName;
        }

        _spawnedMachines.Add(toy);
        var toyName = (toy.GameObject != null) ? toy.GameObject.name : "<null>";
        Console.WriteLine($"[GAMBLE DEBUG] SUCCESS -> physical gamble toy created for '{machine.Id}' at '{machine.Position}', toyName='{toyName}'");

        return toy;
    }

    private bool TryBindMachine(GamblingMachine machine, InteractableToy toy)
    {
        try
        {
            Console.WriteLine($"[GAMBLE DEBUG] BindToy start -> machine='{machine.Id}', toyType='{toy.GetType().FullName}', toyPosition='{toy.Position}', toyInstance='{toy}'");
            machine.BindToy(toy);
            Console.WriteLine($"[GAMBLE DEBUG] BindToy result -> machine='{machine.Id}', boundToyIsNull={machine.BoundToy == null}, boundToy='{machine.BoundToy}'");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAMBLE DEBUG] BindToy FAIL -> machine='{machine.Id}', error='{ex.Message}'");
            return false;
        }
    }
}
