using System.Collections.Generic;
using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories;

/// <summary>
/// Settings for behavior that only exists in this fork (reload handling, door handling).
/// </summary>
[DataContract]
public class ForkSettings : SAINSettingsBase<ForkSettings>, ISAINSettings
{
    // ---------- Reloading ----------

    [DataMember]
    [Name("Random Suppress Cutoff")]
    [Description(
        "Each magazine, a bot stops suppressing your last known position at a different ammo level, so going quiet does not reliably mean it is reloading. Off = every bot stops at the Fixed Suppress Cutoff."
    )]
    public bool RandomSuppressCutoff = true;

    [DataMember]
    [Name("Suppress Cutoff Min")]
    [Description("Lowest ammo ratio (0-1) at which a bot stops suppressing. 0.25 = it can fire down to a quarter magazine.")]
    [MinMax(0f, 1f, 100f)]
    public float SuppressCutoffMin = 0.25f;

    [DataMember]
    [Name("Suppress Cutoff Max")]
    [Description("Highest ammo ratio (0-1) at which a bot stops suppressing. Higher = some bots go quiet while still mostly loaded.")]
    [MinMax(0f, 1f, 100f)]
    public float SuppressCutoffMax = 0.65f;

    [DataMember]
    [Name("Fixed Suppress Cutoff")]
    [Description("Ammo ratio (0-1) at which bots stop suppressing when Random Suppress Cutoff is off. Original SAIN value: 0.33.")]
    [MinMax(0f, 1f, 100f)]
    public float FixedSuppressCutoff = 0.33f;

    [DataMember]
    [Name("Relocate Before Reload")]
    [Description(
        "When a bot starts reloading in close quarters, it leaves its cover and reloads somewhere else instead of staying where you saw it shooting. Off = original behavior."
    )]
    public bool RelocateEnabled = true;

    [DataMember]
    [Name("Relocate Chance")]
    [Description("Chance (0-1) that a bot relocates when the conditions are met. Below 1 keeps it from being a pattern.")]
    [MinMax(0f, 1f, 100f)]
    public float RelocateChance = 0.6f;

    [DataMember]
    [Name("Relocate Max Enemy Distance")]
    [Description("Only relocate if the enemy is closer than this many meters (close quarters).")]
    [MinMax(1f, 150f, 1f)]
    public float RelocateMaxEnemyDistance = 25f;

    [DataMember]
    [Name("Relocate Max Time Since Seen")]
    [Description("Only relocate if the bot saw the enemy within this many seconds, or was shot at by it.")]
    [MinMax(1f, 120f, 1f)]
    public float RelocateMaxTimeSinceSeen = 15f;

    [DataMember]
    [Name("Relocate Cooldown")]
    [Description("Minimum seconds between relocations for one bot, to stop it running back and forth.")]
    [MinMax(0f, 120f, 1f)]
    public float RelocateCooldown = 8f;

    [DataMember]
    [Name("Relocate Min Distance From Old Cover")]
    [Description("The new cover point must be at least this many meters from the one the bot left.")]
    [MinMax(0f, 30f, 10f)]
    public float RelocateMinDistanceFromOldCover = 3f;

    // ---------- Doors ----------

    [DataMember]
    [Name("Bots Close Doors")]
    [Description(
        "Bots close doors that are already open when one is in their way. Off by default: bots then leave open doors alone instead of closing the door they just opened."
    )]
    public bool BotsCloseDoors = false;

    [DataMember]
    [Name("Close Door When Stuck")]
    [Description(
        "A bot that has stopped making progress next to an open door closes it, which frees a bot wedged against the open leaf. Independent of Bots Close Doors."
    )]
    public bool StuckCloseDoor = true;

    [DataMember]
    [Name("Stuck Close Door Delay")]
    [Description("Seconds without progress before a stuck bot closes a nearby open door.")]
    [MinMax(0.1f, 10f, 10f)]
    public float StuckCloseDoorDelay = 1f;

    [DataMember]
    [Name("Stuck Close Door Range")]
    [Description("A stuck bot only closes open doors within this many meters.")]
    [MinMax(0.5f, 6f, 10f)]
    public float StuckCloseDoorRange = 2.5f;

    [DataMember]
    [Name("Back Away From Door Swing")]
    [Description(
        "When a bot pulls a door open, it walks to a point outside the swing of the door instead of staying where the door hits it. Off = original behavior."
    )]
    public bool DoorRetreatEnabled = true;

    [DataMember]
    [Name("Door Swing Margin")]
    [Description("Extra meters beyond the door's swing circle the bot backs away to.")]
    [MinMax(0f, 3f, 10f)]
    public float DoorRetreatMargin = 0.7f;

    [DataMember]
    [Name("Door Retreat Max Distance")]
    [Description("The most a bot will back away from a door, in meters.")]
    [MinMax(0.5f, 8f, 10f)]
    public float DoorRetreatMaxDistance = 3f;

    [DataMember]
    [Name("Door Retreat Speed")]
    [Description("How fast a bot backs away from a door, in m/s. Low on purpose: backing up is slower than walking forward.")]
    [MinMax(0.2f, 4f, 10f)]
    public float DoorRetreatSpeed = 1f;

    [DataMember]
    [Name("Door Interaction Max Time")]
    [Description("The longest a door interaction (including backing away) can hold the bot, in seconds.")]
    [MinMax(1f, 10f, 10f)]
    public float DoorInteractionMaxTime = 3f;

    public override void Init(List<ISAINSettings> list)
    {
        list.Add(this);
    }
}
