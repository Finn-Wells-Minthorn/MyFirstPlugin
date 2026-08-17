using MyFirstPlugin.Events;

namespace MyFirstPlugin.Config;

public class PluginConfig
{
    public bool AutomaticEventsEnabled { get; set; } = true;

    public EventRollConfig EventRoll { get; set; } = new();

    public BlackoutEventConfig Blackout { get; set; } = new();
}

public class BlackoutEventConfig
{
    public int BlackoutDurationSeconds { get; set; } = 90;

    public int FlickerTransitionDelaySeconds { get; set; } = 41;

    public bool EnableFlickering { get; set; } = true;

    public int FlickerStepDurationMilliseconds { get; set; } = 225;

    public int NormalShortBlackoutSeconds { get; set; } = 30;

    public int NormalPoweredSeconds { get; set; } = 10;

    public int LongBlackoutMinSeconds { get; set; } = 180;

    public int LongBlackoutMaxSeconds { get; set; } = 240;

    public float SubtleFlickerMinIntervalSeconds { get; set; } = 2.5f;

    public float SubtleFlickerMaxIntervalSeconds { get; set; } = 5.5f;

    public float SubtleFlickerDurationSeconds { get; set; } = 0.15f;

    public bool EnableCassieAnnouncement { get; set; } = false;

    public string CassieAnnouncementText { get; set; } = "blackout event activated";

    public string StartAnnouncement { get; set; } = "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>";

    public string EndAnnouncement { get; set; } = "<color=green><b>Power restored. The blackout has ended.</b></color>";

    public ushort StartAnnouncementDurationSeconds { get; set; } = 10;

    public ushort EndAnnouncementDurationSeconds { get; set; } = 5;
}