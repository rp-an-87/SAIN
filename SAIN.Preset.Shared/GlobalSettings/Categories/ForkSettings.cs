using System.Collections.Generic;
using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories;

/// <summary>
/// Settings for behavior that only exists in this fork. Menu layout: Category = "Section/Subsection" (Reload, Doors, Range, Shot Reaction).
/// Range and Shot Reaction each have their own per-brain filter.
/// </summary>
[DataContract]
public class ForkSettings : SAINSettingsBase<ForkSettings>, ISAINSettings
{

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Random Suppress Cutoff")]
    [Description(
        "Each magazine, a bot stops suppressing your last known position at a different ammo level, so going quiet does not reliably mean it is reloading. Off = every bot stops at the Fixed Suppress Cutoff."
    )]
    public bool RandomSuppressCutoff = true;

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Suppress Cutoff Min")]
    [Description("Lowest ammo ratio (0-1) at which a bot stops suppressing. 0.25 = it can fire down to a quarter magazine.")]
    [MinMax(0f, 1f, 100f)]
    public float SuppressCutoffMin = 0.25f;

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Suppress Cutoff Max")]
    [Description("Highest ammo ratio (0-1) at which a bot stops suppressing. Higher = some bots go quiet while still mostly loaded.")]
    [MinMax(0f, 1f, 100f)]
    public float SuppressCutoffMax = 0.65f;

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Fixed Suppress Cutoff")]
    [Description("Ammo ratio (0-1) at which bots stop suppressing when Random Suppress Cutoff is off. Original SAIN value: 0.33.")]
    [MinMax(0f, 1f, 100f)]
    public float FixedSuppressCutoff = 0.33f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Before Reloading")]
    [Description(
        "When a bot starts reloading in close quarters, it leaves its cover and reloads somewhere else instead of staying where you saw it shooting. Off = original behavior."
    )]
    public bool RelocateEnabled = true;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Chance")]
    [Description("Chance (0-1) that a bot relocates when the conditions are met. Below 1 keeps it from being a pattern.")]
    [MinMax(0f, 1f, 100f)]
    public float RelocateChance = 0.6f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Max Enemy Distance")]
    [Description("Only relocate if the enemy is closer than this many meters (close quarters).")]
    [MinMax(1f, 150f, 1f)]
    public float RelocateMaxEnemyDistance = 25f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Max Time Since Seen")]
    [Description("Only relocate if the bot saw the enemy within this many seconds, or was shot at by it.")]
    [MinMax(1f, 120f, 1f)]
    public float RelocateMaxTimeSinceSeen = 15f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Cooldown")]
    [Description("Minimum seconds between relocations for one bot, to stop it running back and forth.")]
    [MinMax(0f, 120f, 1f)]
    public float RelocateCooldown = 8f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Min Distance From Old Cover")]
    [Description("The new cover point must be at least this many meters from the one the bot left.")]
    [MinMax(0f, 30f, 10f)]
    public float RelocateMinDistanceFromOldCover = 3f;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Bots Close Doors")]
    [Description(
        "Bots close doors that are already open when one is in their way. Off by default: bots then leave open doors alone instead of closing the door they just opened."
    )]
    public bool BotsCloseDoors = false;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Close Door When Stuck")]
    [Description(
        "A bot that has stopped making progress next to an open door closes it, which frees a bot wedged against the open leaf. Independent of Bots Close Doors."
    )]
    public bool StuckCloseDoor = true;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Stuck Close Door Delay")]
    [Description("Seconds without progress before a stuck bot closes a nearby open door.")]
    [MinMax(0.1f, 10f, 10f)]
    public float StuckCloseDoorDelay = 1f;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Stuck Close Door Range")]
    [Description("A stuck bot only closes open doors within this many meters.")]
    [MinMax(0.5f, 6f, 10f)]
    public float StuckCloseDoorRange = 2.5f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Back Away From Door Swing")]
    [Description(
        "When a bot pulls a door open, it walks to a point outside the swing of the door instead of staying where the door hits it. Off = original behavior."
    )]
    public bool DoorRetreatEnabled = true;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Swing Margin")]
    [Description("Extra meters beyond the door's swing circle the bot backs away to.")]
    [MinMax(0f, 3f, 10f)]
    public float DoorRetreatMargin = 0.7f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Retreat Max Distance")]
    [Description("The most a bot will back away from a door, in meters.")]
    [MinMax(0.5f, 8f, 10f)]
    public float DoorRetreatMaxDistance = 3f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Retreat Speed")]
    [Description("How fast a bot backs away from a door, in m/s. Low on purpose: backing up is slower than walking forward.")]
    [MinMax(0.2f, 4f, 10f)]
    public float DoorRetreatSpeed = 1f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Interaction Max Time")]
    [Description("The longest a door interaction (including backing away) can hold the bot, in seconds.")]
    [MinMax(1f, 10f, 10f)]
    public float DoorInteractionMaxTime = 3f;

    [DataMember]
    [Category("Range/General")]
    [Name("Sight-Adjusted Reach")]
    [Description(
        "A bot's effective reach (how far it stands and shoots) is its weapon class reach multiplied by a factor for its sights and scope zoom. Off = reach depends on the weapon class only (original behavior)."
    )]
    public bool SightReachEnabled = true;

    [DataMember]
    [Category("Range/General")]
    [Name("Shoot Reach Factor")]
    [Description(
        "A bot only aims and fires at a visible enemy closer than its effective reach times this value (includes distant-fire bursts). 0 = no limit (original behavior). The original stand-and-shoot rule already uses 1.25."
    )]
    [MinMax(0f, 3f, 100f)]
    public float ShootReachFactor = 1.25f;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: PMCs")]
    [Description("PMCs (USEC and BEAR) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainPmc = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Rogues")]
    [Description("Rogues (exUsec, Lighthouse) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainRogue = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Raiders")]
    [Description("Raiders (pmcBot: Labs, Reserve) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainRaider = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Scavs")]
    [Description("Regular scavs, scav groups and tagged-and-cursed scavs use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainScav = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Scav Snipers")]
    [Description("Scav snipers (marksman) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainScavSniper = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Cultists")]
    [Description("Cultists use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainCultist = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Bosses")]
    [Description("Bosses (not the Goons) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainBoss = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Boss Followers")]
    [Description("Boss guards (not the Goons) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainFollower = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Goons")]
    [Description("Knight, Big Pipe and Birdeye use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainGoon = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Event Bots")]
    [Description("Event bots (crazy scav event, gifter) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainEvent = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Other Bots")]
    [Description("Any bot type not listed above (zombies, BTR, arena fighters, Labyrinth bots, ...) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainOther = false;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Iron Sights Reach Factor")]
    [Description("Reach multiplier for a weapon with no optic and no red dot. 0.6 turns a 150 m assault rifle into a 90 m weapon.")]
    [MinMax(0.1f, 2f, 100f)]
    public float IronSightReach = 0.6f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Red Dot Reach Factor")]
    [Description("Reach multiplier for a weapon with a red dot or holographic sight (no magnified optic).")]
    [MinMax(0.1f, 2f, 100f)]
    public float RedDotReach = 0.75f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Optic Reach Factor At 1x")]
    [Description("Reach multiplier for an optic at 1x zoom. Every extra x of zoom adds Optic Reach Per Zoom.")]
    [MinMax(0.1f, 2f, 100f)]
    public float OpticReachAt1x = 0.8f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Optic Reach Per Zoom")]
    [Description("Added to the optic reach factor for every x of zoom above 1. 0.1 gives a 4x scope a factor of 1.1 and an 8x scope 1.5.")]
    [MinMax(0f, 0.5f, 100f)]
    public float OpticReachPerZoom = 0.1f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Optic Reach Factor Max")]
    [Description("The highest reach multiplier a scope can give.")]
    [MinMax(0.5f, 4f, 100f)]
    public float OpticReachMax = 1.6f;

    [DataMember]
    [Category("Range/Suppress limit")]
    [Name("Suppress Limit")]
    [Description("Bots do not fire suppressing bursts at a last known position farther away than their effective reach times Suppress Reach Fraction. Off = no limit (original behavior).")]
    public bool SuppressLimitEnabled = true;

    [DataMember]
    [Category("Range/Suppress limit")]
    [Name("Suppress Reach Fraction")]
    [Description("Maximum suppress distance as a fraction of the bot's effective reach. 0.8 of a 150 m reach is 120 m.")]
    [MinMax(0.1f, 2f, 100f)]
    public float SuppressReachFraction = 0.8f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Fire")]
    [Description(
        "A bot that sees an enemy beyond its shooting reach (reach times Shoot Reach Factor) still gets a small chance to shoot, falling with distance. Off = a hard cut-off at that distance."
    )]
    public bool OverReachEnabled = true;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Chance")]
    [Description("Chance, rolled once per interval, that the bot fires at an enemy just past its reach. Falls linearly to 0 at the limit below.")]
    [MinMax(0f, 1f, 100f)]
    public float OverReachChance = 0.5f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Limit Factor")]
    [Description("Beyond this multiple of the bot's reach it never fires. Must be above Shoot Reach Factor.")]
    [MinMax(1f, 5f, 100f)]
    public float OverReachLimitFactor = 4f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Roll Interval")]
    [Description("Seconds between over-reach rolls for a bot. Longer = fewer, rarer bursts.")]
    [MinMax(1f, 30f, 1f)]
    public float OverReachRollInterval = 6f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Window")]
    [Description("A successful roll lets the bot fire for about this many seconds (varies by a third either way).")]
    [MinMax(0.5f, 15f, 10f)]
    public float OverReachWindow = 3f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Scatter")]
    [Description(
        "Shots beyond the bot's reach scatter this many times more, unless the bot has had you in continuous view for the time below. 1 = no extra scatter."
    )]
    [MinMax(1f, 10f, 10f)]
    public float OverReachScatterMulti = 4f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Accurate After")]
    [Description("Seconds of continuous view of the enemy after which an over-reach shot is as accurate as normal (the bot has really tracked you).")]
    [MinMax(0f, 30f, 10f)]
    public float OverReachAccurateAfter = 5f;

    [DataMember]
    [Category("Shot Reaction/General")]
    [Name("Shot Reaction")]
    [Description(
        "Master switch. A bot shot at by an enemy it cannot see rolls a reaction (take cover, fight back after a delay, suppress, or hunt the shooter) instead of instantly turning on you. The position it learns from the shot is vague, and the shooter is forgotten if the bot never sees it again. Off = original behavior."
    )]
    public bool ShotReactionEnabled = true;

    [DataMember]
    [Category("Shot Reaction/General")]
    [Name("Shot Reaction: Player Shooters Only")]
    [Description("Only react this way to shots from you. Shots from other bots keep the original behavior, which also keeps the cost down.")]
    public bool ShotReactionPlayerOnly = true;

    [DataMember]
    [Category("Shot Reaction/General")]
    [Name("Shot Reaction: Log Rolls")]
    [Description("Write every reaction roll (distance, reach, weights, outcome) to the BepInEx log. For tuning.")]
    public bool ShotReactionLog = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: PMCs")]
    [Description("PMCs (USEC and BEAR) use the fork's shot reaction.")]
    public bool ShotBrainPmc = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Rogues")]
    [Description("Rogues (exUsec, Lighthouse) use the fork's shot reaction.")]
    public bool ShotBrainRogue = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Raiders")]
    [Description("Raiders (pmcBot: Labs, Reserve) use the fork's shot reaction.")]
    public bool ShotBrainRaider = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Scavs")]
    [Description("Regular scavs, scav groups and tagged-and-cursed scavs use the fork's shot reaction.")]
    public bool ShotBrainScav = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Scav Snipers")]
    [Description("Scav snipers (marksman) use the fork's shot reaction.")]
    public bool ShotBrainScavSniper = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Cultists")]
    [Description("Cultists use the fork's shot reaction.")]
    public bool ShotBrainCultist = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Bosses")]
    [Description("Bosses (not the Goons) use the fork's shot reaction.")]
    public bool ShotBrainBoss = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Boss Followers")]
    [Description("Boss guards (not the Goons) use the fork's shot reaction.")]
    public bool ShotBrainFollower = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Goons")]
    [Description("Knight, Big Pipe and Birdeye use the fork's shot reaction.")]
    public bool ShotBrainGoon = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Event Bots")]
    [Description("Event bots (crazy scav event, gifter) use the fork's shot reaction.")]
    public bool ShotBrainEvent = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Other Bots")]
    [Description("Any bot type not listed above (zombies, BTR, arena fighters, Labyrinth bots, ...) use the fork's shot reaction.")]
    public bool ShotBrainOther = false;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Short Zone End")]
    [Description("Shots from closer than this fraction of the bot's effective reach use the Short weights.")]
    [MinMax(0.05f, 1f, 100f)]
    public float ShotZoneShort = 0.25f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Mid Zone End")]
    [Description("Shots from closer than this fraction of the bot's effective reach (and farther than the short zone) use the Mid weights. Farther = Long weights.")]
    [MinMax(0.1f, 3f, 100f)]
    public float ShotZoneMid = 0.75f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Short: Cover Weight")]
    [Description("Relative weight of taking cover when the shooter is in the short zone. The four weights of a zone are compared, they do not need to add up to 100.")]
    [MinMax(0f, 100f, 1f)]
    public float ShortCover = 45f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Short: Fight Weight")]
    [Description("Relative weight of fighting back (after a reaction delay) in the short zone.")]
    [MinMax(0f, 100f, 1f)]
    public float ShortFight = 35f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Short: Suppress Weight")]
    [Description("Relative weight of taking cover and firing suppressing bursts in the short zone.")]
    [MinMax(0f, 100f, 1f)]
    public float ShortSuppress = 5f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Short: Hunt Weight")]
    [Description("Relative weight of taking cover, assessing, then hunting the shooter in the short zone.")]
    [MinMax(0f, 100f, 1f)]
    public float ShortHunt = 15f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Mid: Cover Weight")]
    [Description("Relative weight of taking cover in the mid zone.")]
    [MinMax(0f, 100f, 1f)]
    public float MidCover = 60f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Mid: Fight Weight")]
    [Description("Relative weight of fighting back in the mid zone.")]
    [MinMax(0f, 100f, 1f)]
    public float MidFight = 8f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Mid: Suppress Weight")]
    [Description("Relative weight of cover plus suppressing bursts in the mid zone.")]
    [MinMax(0f, 100f, 1f)]
    public float MidSuppress = 12f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Mid: Hunt Weight")]
    [Description("Relative weight of cover, assess, hunt in the mid zone.")]
    [MinMax(0f, 100f, 1f)]
    public float MidHunt = 20f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Long: Cover Weight")]
    [Description("Relative weight of taking cover in the long zone.")]
    [MinMax(0f, 100f, 1f)]
    public float LongCover = 85f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Long: Fight Weight")]
    [Description("Relative weight of fighting back in the long zone. Only bots whose weapon reaches the shooter can pick it.")]
    [MinMax(0f, 100f, 1f)]
    public float LongFight = 2f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Long: Suppress Weight")]
    [Description("Relative weight of cover plus suppressing bursts in the long zone. Still limited by the suppress distance limit.")]
    [MinMax(0f, 100f, 1f)]
    public float LongSuppress = 3f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Long: Hunt Weight")]
    [Description("Relative weight of cover, assess, hunt in the long zone.")]
    [MinMax(0f, 100f, 1f)]
    public float LongHunt = 10f;

    [DataMember]
    [Category("Shot Reaction/Pulls")]
    [Name("Cover Min Share")]
    [Description("The share (0-1) of rolls that always goes to Cover, whatever the personality and weapon pull. Keeps bots from always fighting back.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotCoverMinShare = 0.35f;

    [DataMember]
    [Category("Shot Reaction/Pulls")]
    [Name("Fight Max Share")]
    [Description("The most (0-1) the Fight outcome can get of a roll, whatever the personality and weapon pull.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFightMaxShare = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Pulls")]
    [Name("Aggressive Personality Pull")]
    [Description("Wreckless, Giga Chad and Chad multiply their Fight, Suppress and Hunt weights by this and divide their Cover weight by it.")]
    [MinMax(1f, 4f, 10f)]
    public float ShotAggressivePull = 1.5f;

    [DataMember]
    [Category("Shot Reaction/Pulls")]
    [Name("Timid Personality Pull")]
    [Description("Rat, Timmy and Coward multiply their Cover weight by this and divide their Fight, Suppress and Hunt weights by it.")]
    [MinMax(1f, 4f, 10f)]
    public float ShotTimidPull = 1.5f;

    [DataMember]
    [Category("Shot Reaction/Pulls")]
    [Name("Suppressed Shooter Factor")]
    [Description("Fight, Suppress and Hunt weights are multiplied by this when the shooter uses a suppressor (harder to place).")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotSuppressedShooterFactor = 0.6f;

    [DataMember]
    [Category("Shot Reaction/Pulls")]
    [Name("Prone Shooter Factor")]
    [Description("Fight, Suppress and Hunt weights are multiplied by this when the shooter is prone (harder to place).")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotProneShooterFactor = 0.7f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Position Error Per Meter")]
    [Description("How wrong the position a bot learns from a shot is, in meters of error per meter of distance. 0.2 = 8 m error at 40 m, 60 m error at 300 m.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotPositionErrorPerMeter = 0.2f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Position Error Max")]
    [Description("The largest position error, in meters.")]
    [MinMax(0f, 200f, 1f)]
    public float ShotPositionErrorMax = 80f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Hit Position Error Factor")]
    [Description("The position error is multiplied by this when the bot was actually hit (a hit says more about where the shooter is).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotHitErrorFactor = 0.4f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Fight Duration")]
    [Description("A bot that chose to fight back uses normal SAIN combat behavior at once for this many seconds, then the shooter is forgotten if the bot has not seen it.")]
    [MinMax(2f, 60f, 1f)]
    public float ShotFightDuration = 10f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Cover Time Min")]
    [Description("Shortest time, in seconds, a bot keeps to cover after choosing Cover or Suppress (for a close shooter).")]
    [MinMax(0.5f, 30f, 10f)]
    public float ShotCoverTimeMin = 3f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Cover Time Max")]
    [Description("Longest time, in seconds, a bot keeps to cover after choosing Cover or Suppress (for a shooter at the edge of its reach or beyond).")]
    [MinMax(0.5f, 60f, 10f)]
    public float ShotCoverTimeMax = 10f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Forget Delay After Cover")]
    [Description("A bot that chose Cover or Suppress forgets the shooter this many seconds after leaving cover, unless it was shot at again or saw the shooter.")]
    [MinMax(0f, 30f, 10f)]
    public float ShotForgetDelay = 2f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Assess Time Min")]
    [Description("Shortest time, in seconds, a bot that chose to hunt stays in cover assessing before it moves out.")]
    [MinMax(0.5f, 30f, 10f)]
    public float ShotAssessTimeMin = 3f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Assess Time Max")]
    [Description("Longest time, in seconds, a bot that chose to hunt stays in cover assessing before it moves out.")]
    [MinMax(0.5f, 60f, 10f)]
    public float ShotAssessTimeMax = 8f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Hunt Time Max")]
    [Description("A bot that chose to hunt gives up after this many seconds from the shot if it has not seen the shooter.")]
    [MinMax(5f, 180f, 1f)]
    public float ShotHuntTimeMax = 40f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Reroll Interval")]
    [Description("Minimum seconds between two reaction rolls for one bot. Shots in between only keep the current reaction going.")]
    [MinMax(0.5f, 30f, 10f)]
    public float ShotRerollInterval = 4f;

    [DataMember]
    [Category("Shot Reaction/Under fire")]
    [Name("Under Fire Spot Coefficient")]
    [Description(
        "Multiplier on the time a bot needs to spot a shooter that is firing at it, when Shot Reaction is on for that bot. Original SAIN value: 0.5 (spots twice as fast). 1 = no bonus for being shot at."
    )]
    [MinMax(0.1f, 2f, 100f)]
    public float ShotUnderFireSpotCoef = 1f;

    [DataMember]
    [Category("Shot Reaction/Under fire")]
    [Name("Under Fire Vision Multiplier")]
    [Description("Multiplier on the distance a bot sees a shooter that is firing at it, when Shot Reaction is on for that bot. Original SAIN value: 1.5. 1 = no bonus.")]
    [MinMax(0.5f, 3f, 100f)]
    public float ShotUnderFireVisionMulti = 1f;

    [DataMember]
    [Category("Shot Reaction/Relocate after hiding")]
    [Name("Shot Relocate After Hiding")]
    [Description(
        "After a bot that hid from an unseen shooter (Cover or Suppress outcome) is done hiding, it walks away to a different area instead of returning to where it was shot at, then resumes its task from there."
    )]
    public bool ShotRelocateEnabled = true;

    [DataMember]
    [Category("Shot Reaction/After hiding")]
    [Name("Follow-up: Fight On")]
    [Description(
        "Weight: after hiding, the bot stays in cover and fights back (suppressive fire at the vague area, normal combat if it sees the shooter). Personality and weapon reach scale the weights."
    )]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowFightOn = 0.4f;

    [DataMember]
    [Category("Shot Reaction/After hiding")]
    [Name("Follow-up: Investigate")]
    [Description("Weight: after hiding, the bot goes looking for the shooter (normal search toward the vague area, stops and fights when it sees the shooter).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowInvestigate = 0.3f;

    [DataMember]
    [Category("Shot Reaction/After hiding")]
    [Name("Follow-up: Retreat")]
    [Description("Weight: after hiding, the bot relocates away from the shot area (see the Relocate settings). Ignored if Relocate is off.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowRetreat = 0.3f;

    [DataMember]
    [Category("Shot Reaction/After hiding")]
    [Name("Fight-On Duration")]
    [Description("Seconds the bot keeps fighting back from cover after hiding, before it may forget the shooter.")]
    [MinMax(2f, 60f, 10f)]
    public float ShotFightOnDuration = 15f;

    [DataMember]
    [Category("Shot Reaction/Relocate after hiding")]
    [Name("Shot Relocate Min Distance")]
    [Description("Shortest distance in meters of the new area from where the bot hid.")]
    [MinMax(10f, 150f, 1f)]
    public float ShotRelocateMinDistance = 40f;

    [DataMember]
    [Category("Shot Reaction/Relocate after hiding")]
    [Name("Shot Relocate Max Distance")]
    [Description("Longest distance in meters of the new area from where the bot hid.")]
    [MinMax(10f, 250f, 1f)]
    public float ShotRelocateMaxDistance = 80f;

    [DataMember]
    [Category("Shot Reaction/Relocate after hiding")]
    [Name("Shot Relocate Time Limit")]
    [Description("The bot gives up relocating and goes back to normal behavior after this many seconds.")]
    [MinMax(10f, 180f, 1f)]
    public float ShotRelocateTimeMax = 60f;

    public override void Init(List<ISAINSettings> list)
    {
        list.Add(this);
    }
}
