using System;
using System.Timers;

namespace MyFirstPlugin.Events;

public static class EventTiming
{
    public static Timer CreateTimer(double intervalMs, ElapsedEventHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var timer = new Timer(intervalMs)
        {
            AutoReset = true
        };

        timer.Elapsed += handler;
        return timer;
    }

    public static Timer CreateOneShotTimer(double delayMs, ElapsedEventHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var timer = new Timer(delayMs)
        {
            AutoReset = false
        };

        timer.Elapsed += handler;
        return timer;
    }

    public static void RegisterTimer(EventBase eventInstance, Timer timer)
    {
        if (eventInstance == null)
            throw new ArgumentNullException(nameof(eventInstance));

        if (timer == null)
            throw new ArgumentNullException(nameof(timer));

        eventInstance.TrackTimer(timer);
    }

    public static void RegisterCleanup(EventBase eventInstance, Action cleanupAction)
    {
        if (eventInstance == null)
            throw new ArgumentNullException(nameof(eventInstance));

        if (cleanupAction == null)
            throw new ArgumentNullException(nameof(cleanupAction));

        eventInstance.TrackCleanupAction(cleanupAction);
    }
}
