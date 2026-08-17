namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    public override string Name => "Test Event";

    public override string Description =>
        "A test event used to verify that the event system works.";

    public override void Start()
    {
        // We'll add actual test behavior here later.
    }

    public override void Stop()
    {
        // Cleanup will go here later.
    }
}