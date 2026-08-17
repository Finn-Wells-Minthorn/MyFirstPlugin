using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using System;
using MyFirstPlugin.Handlers;
using MyFirstPlugin.Events;

namespace MyFirstPlugin;

public class MyFirstPlugin : Plugin
{
    public override string Name => "SCP Event System";

    public override string Author => "Your Name";

    public override string Description =>
        "Event system for the server.";

    public override Version Version => new(0, 1, 0);

    public override Version RequiredApiVersion =>
        new(LabApiProperties.CompiledVersion);

    private readonly RoundHandler _roundHandler = new();

    public override void Enable()
    {
        EventManager.Register(new BlackoutEvent());
        CustomHandlersManager.RegisterEventsHandler(_roundHandler);

        Console.WriteLine("[SCPEventSystem] Enabled!");
    }

    public override void Disable()
    {
        EventManager.StopCurrentEvent();

        CustomHandlersManager.UnregisterEventsHandler(_roundHandler);

        Console.WriteLine("[SCPEventSystem] Disabled!");
    }
}