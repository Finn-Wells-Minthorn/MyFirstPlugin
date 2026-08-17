using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using MyFirstPlugin.Events;

namespace MyFirstPlugin.Handlers;

public class RoundHandler : CustomEventsHandler
{
    private readonly EventSelector _eventSelector = new();

    public override void OnServerRoundStarted()
    {
        Logger.Info("[SCPEventSystem] Round started.");

        if (!global::MyFirstPlugin.MyFirstPlugin.AutomaticEventsEnabled)
        {
            Logger.Info("[SCPEventSystem] Automatic events are disabled; skipping auto-selection.");
            return;
        }

        if (EventManager.CurrentEvent != null)
        {
            Logger.Info("[SCPEventSystem] An event is already active for this round; skipping auto-selection.");
            return;
        }

        EventBase? selectedEvent = _eventSelector.Select();
        if (selectedEvent == null)
        {
            Logger.Warn("[SCPEventSystem] No enabled events are currently available.");
            return;
        }

        EventBase? startedEvent = EventManager.StartEvent(selectedEvent);
        if (startedEvent == null)
        {
            Logger.Warn($"[SCPEventSystem] Failed to start selected event: {selectedEvent.Name}");
            return;
        }

        Logger.Info(
            $"[SCPEventSystem] Selected event: {startedEvent.Name} - {startedEvent.Description}");
    }

    public override void OnServerRoundRestarted()
    {
        Logger.Info("[SCPEventSystem] Round restarting.");

        EventManager.StopCurrentEvent();
    }
}