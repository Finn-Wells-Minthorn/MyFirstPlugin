using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFirstPlugin.Events;

public static class EventManager
{
    private static readonly Dictionary<string, EventBase> Registered = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Random Random = new();

    public static EventBase? CurrentEvent { get; private set; }

    public static IReadOnlyCollection<EventBase> RegisteredEvents => Registered.Values;

    public static void Register(EventBase eventInstance)
    {
        if (eventInstance == null)
            throw new ArgumentNullException(nameof(eventInstance));

        if (Registered.ContainsKey(eventInstance.Name))
            throw new InvalidOperationException($"An event with the name '{eventInstance.Name}' is already registered.");

        Registered[eventInstance.Name] = eventInstance;
    }

    public static bool Unregister(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return false;

        if (CurrentEvent != null && string.Equals(CurrentEvent.Name, eventName, StringComparison.OrdinalIgnoreCase))
            StopCurrentEvent();

        return Registered.Remove(eventName);
    }

    public static bool Unregister(EventBase eventInstance)
    {
        if (eventInstance == null)
            return false;

        return Unregister(eventInstance.Name);
    }

    public static EventBase? GetEvent(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return null;

        Registered.TryGetValue(eventName, out EventBase? eventInstance);
        return eventInstance;
    }

    public static EventBase? StartEvent(string eventName)
    {
        EventBase? eventInstance = GetEvent(eventName);
        if (eventInstance == null)
            return null;

        return StartEvent(eventInstance);
    }

    public static EventBase? StartEvent(EventBase eventInstance)
    {
        if (eventInstance == null)
            return null;

        if (!eventInstance.Enabled)
            return null;

        if (CurrentEvent != null && CurrentEvent != eventInstance)
            StopCurrentEvent();

        if (CurrentEvent == eventInstance && eventInstance.IsRunning)
            return CurrentEvent;

        CurrentEvent = eventInstance;
        eventInstance.Start();

        return CurrentEvent;
    }

    public static EventBase? StopCurrentEvent()
    {
        if (CurrentEvent == null)
            return null;

        EventBase current = CurrentEvent;
        CurrentEvent = null;
        current.Stop();

        return current;
    }

    public static EventBase? SelectRandomEvent()
    {
        List<EventBase> available = Registered.Values
            .Where(x => x.Enabled)
            .ToList();

        if (available.Count == 0)
            return null;

        return available[Random.Next(available.Count)];
    }

    public static EventBase? StartRandomEvent()
    {
        EventBase? selectedEvent = SelectRandomEvent();
        if (selectedEvent == null)
            return null;

        return StartEvent(selectedEvent);
    }
}