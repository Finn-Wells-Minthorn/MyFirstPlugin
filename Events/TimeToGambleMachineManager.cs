using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public sealed class TimeToGambleMachineManager
{
    private readonly List<GamblingMachine> _machines = new();
    private bool _subscribed;

    public IReadOnlyCollection<GamblingMachine> Machines => _machines;

    public void RegisterMachine(GamblingMachine machine)
    {
        if (machine == null)
            throw new ArgumentNullException(nameof(machine));

        _machines.Add(machine);
    }

    public void Clear()
    {
        _machines.Clear();
    }

    public void Subscribe()
    {
        if (_subscribed)
            return;

        PlayerEvents.InteractedToy += OnPlayerInteractedToy;
        _subscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_subscribed)
            return;

        PlayerEvents.InteractedToy -= OnPlayerInteractedToy;
        _subscribed = false;
    }

    private void OnPlayerInteractedToy(PlayerInteractedToyEventArgs args)
    {
        if (args == null)
            return;

        Player player = args.Player;
        if (player == null)
            return;

        InteractableToy toy = args.Interactable;
        if (toy == null)
            return;

        GamblingMachine? machine = FindMachineForToy(toy);
        if (machine == null)
            return;

        if (!machine.TryUse(player, out string reason))
        {
            player.SendHint(reason, 3f);
            return;
        }

        player.SendHint("Machine activated.", 3f);
    }

    private GamblingMachine? FindMachineForToy(InteractableToy toy)
    {
        foreach (GamblingMachine machine in _machines)
        {
            if (machine.Matches(toy))
                return machine;
        }

        return null;
    }
}
