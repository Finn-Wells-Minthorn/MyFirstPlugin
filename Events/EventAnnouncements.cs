using LabApi.Features.Wrappers;

namespace MyFirstPlugin.Events;

public static class EventAnnouncements
{
    public static void Broadcast(string message, ushort duration = 5)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Server.SendBroadcast(message, duration);
    }
}
