using LabApi.Features.Wrappers;
using System.Threading.Tasks;

namespace MyFirstPlugin.Events;

public class TestEvent : EventBase
{
    public override string Name => "Flashlight Test Event";

    public override string Description =>
        "A test event used to verify that the event system works with flashlights.";

    public override void Start()
    {
        Server.SendBroadcast(
            "<color=green><b>TEST EVENT ACTIVATED!</b></color>",
            10
        );
        
        {
            foreach (Player player in Player.List)
            {
                player.AddItem(ItemType.Flashlight);
            }
        }
    }

    public override void Stop()
    {
        // Cleanup will go here
    }
}