using System;
using System.Collections.Generic;

namespace MyFirstPlugin.Events;

public static class EventManager
{
    private static readonly List<EventBase> Events = new()
    {
        new TestEvent()
    };

    private static readonly Random Random = new();

    public static EventBase? CurrentEvent { get; private set; }

    public static IReadOnlyList<EventBase> RegisteredEvents => Events;

    public static EventBase? SelectRandomEvent()
    {
        List<EventBase> availableEvents = Events.FindAll(x => x.Enabled);

        if (availableEvents.Count == 0)
            return null;

        return availableEvents[Random.Next(availableEvents.Count)];
    }

    public static EventBase? StartRandomEvent()
    {
        EventBase? selectedEvent = SelectRandomEvent();

        if (selectedEvent == null)
            return null;

        CurrentEvent = selectedEvent;
        CurrentEvent.Start();

        return CurrentEvent;
    }

    public static void StopCurrentEvent()
    {
        if (CurrentEvent == null)
            return;

        CurrentEvent.Stop();
        CurrentEvent = null;
    }
}