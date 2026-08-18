using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using System;
using MyFirstPlugin.Commands;
using MyFirstPlugin.Config;
using MyFirstPlugin.Handlers;
using MyFirstPlugin.Events;

namespace MyFirstPlugin;

public class MyFirstPlugin : Plugin<PluginConfig>
{
    public static MyFirstPlugin? Instance { get; private set; }

    public static bool AutomaticEventsEnabled =>
        Instance == null ? true : Instance.Config.AutomaticEventsEnabled;

    public override string Name => "SCP Event System";

    public override string Author => "Your Name";

    public override string Description =>
        "Event system for the server.";

    public override Version Version => new(0, 1, 0);

    public override Version RequiredApiVersion =>
        new(LabApiProperties.CompiledVersion);

    private readonly RoundHandler _roundHandler = new();
    private bool _commandsRegistered;

    private void RegisterEvents()
    {
        EventManager.Register(new BlackoutEvent(Config.Blackout));
        EventManager.Register(new TimeToGambleEvent(Config.TimeToGamble));
        EventManager.Register(new SpeedDemonEvent(Config.SpeedDemon));
    }

    public override void Enable()
    {
        Instance = this;

        RegisterEvents();
        RegisterCommands();
        CustomHandlersManager.RegisterEventsHandler(_roundHandler);

        Console.WriteLine("[SCPEventSystem] Enabled!");
    }

    private void RegisterCommands()
    {
        if (_commandsRegistered)
            return;

        CommandLoader.RegisterCommands(typeof(EventCommand), Name);
        _commandsRegistered = true;
    }

    public override void Disable()
    {
        EventManager.StopCurrentEvent();

        CustomHandlersManager.UnregisterEventsHandler(_roundHandler);
        Instance = null;

        Console.WriteLine("[SCPEventSystem] Disabled!");
    }
}