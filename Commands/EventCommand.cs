using System;
using System.Linq;
using CommandSystem;
using MyFirstPlugin.Events;

namespace MyFirstPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class EventCommand : ICommand
{
    public string Command => "event";

    public string[] Aliases => new[] { "events" };

    public string Description => "Lists available events and starts or stops the active one.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string[] args = arguments.Array == null ? Array.Empty<string>() : arguments.ToArray();

        if (args.Length == 0)
        {
            response = BuildUsage();
            return true;
        }

        switch (args[0].Trim().ToLowerInvariant())
        {
            case "list":
                response = GetListResponse();
                return true;

            case "current":
                response = GetCurrentResponse();
                return true;

            case "stop":
                response = StopCurrentEvent();
                return true;

            case "start":
                if (args.Length < 2)
                {
                    response = "Usage: /event start <event>\n" + GetListResponse();
                    return true;
                }

                response = StartEvent(args[1]);
                return true;

            case "help":
            default:
                response = BuildUsage();
                return true;
        }
    }

    private static string BuildUsage()
    {
        return "Usage: /event <list|current|start <event>|stop>\n" + GetListResponse();
    }

    private static string GetListResponse()
    {
        if (EventManager.RegisteredEvents.Count == 0)
            return "No events are available.";

        return "Available events: " + string.Join(", ", EventManager.RegisteredEvents.Select(x => x.Name));
    }

    private static string GetCurrentResponse()
    {
        EventBase? current = EventManager.CurrentEvent;
        return current == null ? "No event is currently running." : $"Current event: {current.Name}";
    }

    private static string StopCurrentEvent()
    {
        EventBase? stopped = EventManager.StopCurrentEvent();
        return stopped == null ? "No event is currently running." : $"Stopped event: {stopped.Name}";
    }

    private static string StartEvent(string eventName)
    {
        EventBase? target = EventManager.GetEvent(eventName);
        if (target == null)
        {
            return $"Event '{eventName}' was not found. " + GetListResponse();
        }

        EventBase? started = EventManager.StartEvent(target);
        return started == null ? $"Failed to start event '{target.Name}'." : $"Started event: {started.Name}";
    }
}