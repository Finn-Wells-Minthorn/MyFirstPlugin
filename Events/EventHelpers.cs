using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public static class EventHelpers
{
    public static bool IsValidPlayer(Player? player)
    {
        return player != null && !player.IsDestroyed;
    }

    public static List<Player> GetConnectedPlayers()
    {
        return Player.List
            .Where(IsValidPlayer)
            .ToList();
    }

    public static void ForEachConnectedPlayer(Action<Player> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        foreach (Player player in GetConnectedPlayers())
        {
            action(player);
        }
    }
}
