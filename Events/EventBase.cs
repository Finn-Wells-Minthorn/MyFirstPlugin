using System;
using System.Collections.Generic;

namespace MyFirstPlugin.Events;

public abstract class EventBase
{
    private readonly List<IDisposable> _subscriptions = new();
    private readonly List<System.Timers.Timer> _timers = new();

    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual bool Enabled => true;

    public bool IsRunning { get; private set; }

    public virtual void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        OnStart();
    }

    public virtual void Stop()
    {
        if (!IsRunning)
            return;

        OnStop();
        Cleanup();
        IsRunning = false;
    }

    protected virtual void OnStart()
    {
    }

    protected virtual void OnStop()
    {
    }

    protected void TrackTimer(System.Timers.Timer timer)
    {
        if (timer == null)
            throw new ArgumentNullException(nameof(timer));

        _timers.Add(timer);
    }

    protected void TrackSubscription(IDisposable subscription)
    {
        if (subscription == null)
            throw new ArgumentNullException(nameof(subscription));

        _subscriptions.Add(subscription);
    }

    protected void Cleanup()
    {
        foreach (System.Timers.Timer timer in _timers)
        {
            try
            {
                timer.Stop();
                timer.Dispose();
            }
            catch
            {
                // Ignore cleanup errors so a single event can fail without corrupting the registry.
            }
        }

        foreach (IDisposable subscription in _subscriptions)
        {
            try
            {
                subscription.Dispose();
            }
            catch
            {
                // Ignore cleanup errors so a single event can fail without corrupting the registry.
            }
        }

        _timers.Clear();
        _subscriptions.Clear();
    }
}