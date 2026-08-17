using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    public override string Name => "Lantern Test Event";

    public override string Description =>
        "A test event used to verify that the event system works with lanterns.";

    public override void Start()
    {
        Server.SendBroadcast(
            "<color=green><b>TEST EVENT ACTIVATED!</b></color>",
            10
        );

        foreach (Player player in Player.List)
        {
            player.AddItem(ItemType.Lantern);
        }

        Map.TurnOffLights();
    }

    public override void Stop()
    {
        // Cleanup will go here
    }
}