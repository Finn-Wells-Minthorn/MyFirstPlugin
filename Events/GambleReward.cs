using System;

namespace MyFirstPlugin.Events;

public sealed class GambleReward
{
    public GambleReward()
    {
        DisplayName = string.Empty;
        Rarity = string.Empty;
    }

    public GambleReward(
        ItemType itemType,
        string displayName,
        string rarity,
        double weight)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Reward display name cannot be empty.", nameof(displayName));

        if (string.IsNullOrWhiteSpace(rarity))
            throw new ArgumentException("Reward rarity cannot be empty.", nameof(rarity));

        ItemType = itemType;
        DisplayName = displayName;
        Rarity = rarity;
        Weight = weight;
    }

    public ItemType ItemType { get; set; }

    public string DisplayName { get; set; }

    public string Rarity { get; set; }

    public double Weight { get; set; }
}
