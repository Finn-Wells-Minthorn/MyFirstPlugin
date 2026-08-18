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

    public int ShortBlackoutMinSeconds { get; set; } = 25;

    public int ShortBlackoutMaxSeconds { get; set; } = 45;

    public float ShortBlackoutChance { get; set; } = 0.15f;

    public int LongBlackoutMinSeconds { get; set; } = 150;

    public int LongBlackoutMaxSeconds { get; set; } = 210;

    public float BlackoutFlickerChance { get; set; } = 0.65f;

    public float PoweredFlickerChance { get; set; } = 0.55f;

    public float BlackoutFlickerMinIntervalSeconds { get; set; } = 4f;

    public float BlackoutFlickerMaxIntervalSeconds { get; set; } = 15f;

    public float BlackoutFlickerDurationSeconds { get; set; } = 0.12f;

    public float PoweredFlickerMinIntervalSeconds { get; set; } = 3f;

    public float PoweredFlickerMaxIntervalSeconds { get; set; } = 10f;

    public float SubtleFlickerMinIntervalSeconds { get; set; } = 2.5f;

    public float SubtleFlickerMaxIntervalSeconds { get; set; } = 5.5f;

    public float SubtleFlickerDurationSeconds { get; set; } = 0.15f;

    public bool EnableCassieAnnouncement { get; set; } = true;

    public string CassieAnnouncementText { get; set; } = "Attention all personnel. Facility power failure detected.";

    public string StartAnnouncement { get; set; } = "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>";

    public string EndAnnouncement { get; set; } = "<color=green><b>Power restored. The blackout has ended.</b></color>";

    public ushort StartAnnouncementDurationSeconds { get; set; } = 10;

    public ushort EndAnnouncementDurationSeconds { get; set; } = 5;
}