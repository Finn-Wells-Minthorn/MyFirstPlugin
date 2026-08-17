using System.Collections.Generic;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using MyFirstPlugin.Config;
using MyFirstPlugin.Events;

namespace MyFirstPlugin.Handlers;

public class RoundHandler : CustomEventsHandler
{
    private readonly EventSelector _eventSelector = new();
    private EventRollPresenter? _eventRollPresenter;
    private EventStartSequencePresenter? _eventStartSequencePresenter;

    private EventRollPresenter EventRollPresenter =>
        _eventRollPresenter ??= new EventRollPresenter(
            global::MyFirstPlugin.MyFirstPlugin.Instance?.Config?.EventRoll ?? new EventRollConfig());

    private EventStartSequencePresenter EventStartSequencePresenter =>
        _eventStartSequencePresenter ??= new EventStartSequencePresenter();

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

        IReadOnlyList<EventBase> enabledEvents = _eventSelector.GetAvailableEvents();
        if (enabledEvents.Count == 0)
        {
            Logger.Warn("[SCPEventSystem] No enabled events are currently available for the roll.");
            return;
        }

        Logger.Info($"[SCPEventSystem] Event selected for roll: {selectedEvent.Name}");

        EventStartSequencePresenter.Start(() =>
        {
            EventRollPresenter.Start(
                selectedEvent,
                enabledEvents,
                startedEvent =>
                {
                    if (EventManager.CurrentEvent != null)
                        return;

                    EventBase? launchedEvent = EventManager.StartEvent(startedEvent);
                    if (launchedEvent == null)
                    {
                        Logger.Warn($"[SCPEventSystem] Failed to start selected event: {startedEvent.Name}");
                        return;
                    }

                    Logger.Info(
                        $"[SCPEventSystem] Selected event: {launchedEvent.Name} - {launchedEvent.Description}");
                });
        });
    }

    public override void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        Logger.Info("[SCPEventSystem] Round ended.");
        EventStartSequencePresenter.Cancel();
        EventRollPresenter.Cancel();
        EventManager.StopCurrentEvent();
    }

    public override void OnServerRoundRestarted()
    {
        Logger.Info("[SCPEventSystem] Round restarting.");
        EventStartSequencePresenter.Cancel();
        EventRollPresenter.Cancel();
        EventManager.StopCurrentEvent();
    }
}