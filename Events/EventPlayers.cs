using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public static class EventPlayers
{
    public static List<Player> GetActivePlayers()
    {
        return Player.List
            .Where(p => p != null && !p.IsDestroyed && p.IsAlive)
            .ToList();
    }

    public static List<Player> GetValidPlayers()
    {
        return Player.List
            .Where(p => p != null && !p.IsDestroyed)
            .ToList();
    }
}
