using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFirstPlugin.Events;

public interface IEventSelectionStrategy
{
    EventBase? Select(IEnumerable<EventBase> availableEvents);
}

public sealed class RandomEventSelectionStrategy : IEventSelectionStrategy
{
    private readonly Random _random;

    public RandomEventSelectionStrategy()
        : this(new Random())
    {
    }

    public RandomEventSelectionStrategy(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public EventBase? Select(IEnumerable<EventBase> availableEvents)
    {
        List<EventBase> events = availableEvents
            .Where(x => x != null)
            .ToList();

        if (events.Count == 0)
            return null;

        return events[_random.Next(events.Count)];
    }
}

public sealed class EventSelector
{
    private readonly IEventSelectionStrategy _strategy;

    public EventSelector()
        : this(new RandomEventSelectionStrategy())
    {
    }

    public EventSelector(IEventSelectionStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public IReadOnlyList<EventBase> GetAvailableEvents()
    {
        return EventManager.RegisteredEvents
            .Where(x => x.IsEnabled)
            .ToList();
    }

    public EventBase? Select()
    {
        return _strategy.Select(GetAvailableEvents());
    }
}
