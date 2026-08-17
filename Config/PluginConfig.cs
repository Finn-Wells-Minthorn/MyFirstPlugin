namespace MyFirstPlugin.Config;

public class PluginConfig
{
    public bool AutomaticEventsEnabled { get; set; } = true;

    public BlackoutEventConfig Blackout { get; set; } = new();
}

public class BlackoutEventConfig
{
    public int BlackoutDurationSeconds { get; set; } = 90;

    public int FlickerTransitionDelaySeconds { get; set; } = 41;

    public bool EnableFlickering { get; set; } = true;

    public int FlickerStepDurationMilliseconds { get; set; } = 225;

    public string StartAnnouncement { get; set; } = "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>";

    public string EndAnnouncement { get; set; } = "<color=green><b>Power restored. The blackout has ended.</b></color>";

    public ushort StartAnnouncementDurationSeconds { get; set; } = 10;

    public ushort EndAnnouncementDurationSeconds { get; set; } = 5;
}