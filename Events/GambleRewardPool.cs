using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFirstPlugin.Events;

public sealed class GambleRewardPool
{
    private readonly Random _random;
    private readonly List<GambleReward> _rewards;

    public GambleRewardPool(IEnumerable<GambleReward>? rewards = null, Random? random = null)
    {
        _rewards = rewards?.Where(reward => reward != null).ToList() ?? new List<GambleReward>();
        _random = random ?? new Random();
    }

    public IReadOnlyList<GambleReward> Rewards => _rewards;

    public GambleReward? SelectReward()
    {
        double totalWeight = 0d;

        foreach (GambleReward reward in _rewards)
        {
            if (reward.Weight > 0d)
                totalWeight += reward.Weight;
        }

        if (totalWeight <= 0d)
            return null;

        double roll = _random.NextDouble() * totalWeight;
        double cumulativeWeight = 0d;

        foreach (GambleReward reward in _rewards)
        {
            if (reward.Weight <= 0d)
                continue;

            cumulativeWeight += reward.Weight;
            if (roll < cumulativeWeight)
                return reward;
        }

        return _rewards.LastOrDefault(reward => reward.Weight > 0d);
    }
}
