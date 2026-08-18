using System;
using System.Collections.Generic;
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

        GamblingMachine mtfMachine = new GamblingMachine("mtf-gamble-machine", _config.MtfMachinePosition, GamblingMachineTeamType.Mtf, 5f);
        GamblingMachine scientistMachine = new GamblingMachine("scientist-gamble-machine", _config.ScientistMachinePosition, GamblingMachineTeamType.Scientist, 5f);

        InteractableToy? mtfToy = SpawnMachine(mtfMachine, _config.MtfMachineDisplayName);
        InteractableToy? scientistToy = SpawnMachine(scientistMachine, _config.ScientistMachineDisplayName);

        if (mtfToy != null)
        {
            mtfMachine.BindToy(mtfToy);
            _machineManager.RegisterMachine(mtfMachine);
        }

        if (scientistToy != null)
        {
            scientistMachine.BindToy(scientistToy);
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
        Console.WriteLine($"[TimeToGambleEvent] Spawned gambling machine '{machine.Id}' at {machine.Position}.");

        return toy;
    }
}
