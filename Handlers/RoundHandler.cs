using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using MyFirstPlugin.Events;

namespace MyFirstPlugin.Handlers;

public class RoundHandler : CustomEventsHandler
{
    public override void OnServerRoundStarted()
    {
        Logger.Info("[SCPEventSystem] Round started.");

        EventBase? selectedEvent = EventManager.StartRandomEvent();

        if (selectedEvent == null)
        {
            Logger.Warn("[SCPEventSystem] No events are currently available.");
            return;
        }

        Logger.Info(
            $"[SCPEventSystem] Selected event: {selectedEvent.Name} - {selectedEvent.Description}");
    }

    public override void OnServerRoundRestarted()
    {
        Logger.Info("[SCPEventSystem] Round restarting.");

        EventManager.StopCurrentEvent();
    }
}