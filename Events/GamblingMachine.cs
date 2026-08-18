using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public enum GamblingMachineTeamType
{
    Mtf,
    Scientist
}

public sealed class GamblingMachine
{
    private readonly Dictionary<uint, DateTime> _lastUsedByPlayer = new();

    public GamblingMachine(
        string id,
        GamblingMachineTeamType machineType,
        float cooldownSeconds = 5f)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Machine id cannot be empty.", nameof(id));

        Id = id;
        MachineType = machineType;
        CooldownSeconds = Math.Max(0f, cooldownSeconds);
    }

    public string Id { get; }

    public GamblingMachineTeamType MachineType { get; }

    public float CooldownSeconds { get; }

    public Workstation? BoundWorkstation { get; private set; }

    public void BindWorkstation(Workstation workstation)
    {
        if (workstation == null)
            throw new ArgumentNullException(nameof(workstation));

        BoundWorkstation = workstation;
    }

    public bool TryUse(Player player, out string reason)
    {
        reason = string.Empty;

        if (player == null || !player.IsHuman || !player.IsAlive)
        {
            reason = "Only living human players can use this machine.";
            return false;
        }

        if (!CanUse(player, out reason))
            return false;

        if (IsCoolingDown(player))
        {
            reason = "This machine is still cooling down.";
            return false;
        }

        _lastUsedByPlayer[player.NetworkId] = DateTime.UtcNow;
        return true;
    }

    public bool CanUse(Player player, out string reason)
    {
        reason = string.Empty;

        if (player == null)
        {
            reason = "Player is invalid.";
            return false;
        }

        if (!player.IsHuman)
        {
            reason = "Only human players can use this machine.";
            return false;
        }

        if (!player.IsAlive)
        {
            reason = "Only living players can use this machine.";
            return false;
        }

        if (player.Team == PlayerRoles.Team.SCPs || player.Team == PlayerRoles.Team.ClassD)
        {
            reason = "Your team is not allowed to use this machine.";
            return false;
        }

        if (MachineType == GamblingMachineTeamType.Mtf)
        {
            bool isAllowedRole =
                player.Role == PlayerRoles.RoleTypeId.NtfSergeant ||
                player.Role == PlayerRoles.RoleTypeId.NtfCaptain;

            if (!isAllowedRole)
            {
                reason = "Only MTF Sergeants and Captains can use this machine.";
                return false;
            }

            return true;
        }

        if (MachineType == GamblingMachineTeamType.Scientist)
        {
            bool isAllowedRole = player.Role == PlayerRoles.RoleTypeId.Scientist;

            if (!isAllowedRole)
            {
                reason = "Only Scientists can use this machine.";
                return false;
            }

            return true;
        }

        reason = "This machine has no valid assignment.";
        return false;
    }

    public bool IsCoolingDown(Player player)
    {
        if (player == null)
            return true;

        if (CooldownSeconds <= 0f)
            return false;

        if (!_lastUsedByPlayer.TryGetValue(player.NetworkId, out DateTime lastUse))
            return false;

        return (DateTime.UtcNow - lastUse).TotalSeconds < CooldownSeconds;
    }
}
