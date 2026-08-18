using MyFirstPlugin.Events;
using System.Collections.Generic;
using UnityEngine;

namespace MyFirstPlugin.Config;

public class PluginConfig
{
    public bool AutomaticEventsEnabled { get; set; } = true;

    public EventRollConfig EventRoll { get; set; } = new();

    public BlackoutEventConfig Blackout { get; set; } = new();

    public TimeToGambleEventConfig TimeToGamble { get; set; } = new();
}

public class TimeToGambleEventConfig
{
    public MapGeneration.RoomName TargetRoomName { get; set; } = MapGeneration.RoomName.LczArmory;

    public int TargetWorkstationIndex { get; set; } = 0;

    public Vector3 RewardSpawnOffset { get; set; } = new Vector3(0f, 1f, 0f);

    public List<GambleReward> Rewards { get; set; } = new()
    {
        new GambleReward(ItemType.GunE11SR, "E-11 SR", "Rare", 10d),
        new GambleReward(ItemType.Medkit, "Medkit", "Uncommon", 25d),
        new GambleReward(ItemType.Flashlight, "Flashlight", "Common", 45d),
        new GambleReward(ItemType.GrenadeFlash, "Flashbang", "Uncommon", 20d)
    };
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

    public string StartAnnouncement { get; set; } = "<color=red><b>BLACKOUT EVENT ACTIVATED!</b></color>";

    public string PreBlackoutWarning { get; set; } = "<color=red><b>FACILITY POWER FAILURE DETECTED</b></color>";

    public string EndAnnouncement { get; set; } = "<color=green><b>Power restored. The blackout has ended.</b></color>";

    public ushort StartAnnouncementDurationSeconds { get; set; } = 10;

    public ushort PreBlackoutWarningDurationSeconds { get; set; } = 6;

    public ushort EndAnnouncementDurationSeconds { get; set; } = 5;
}