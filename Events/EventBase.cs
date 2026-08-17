namespace MyFirstPlugin.Events;

public abstract class EventBase
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual bool Enabled => true;

    public virtual void Start()
    {
    }

    public virtual void Stop()
    {
    }
}