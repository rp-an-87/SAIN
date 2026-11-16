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
    public bool RangeBrainRaider = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Scavs")]
    [Description("Regular scavs, scav groups and tagged-and-cursed scavs use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainScav = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Scav Snipers")]
    [Description("Scav snipers (marksman) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainScavSniper = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Cultists")]
    [Description("Cultists use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainCultist = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Bosses")]
    [Description("Bosses (not the Goons) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainBoss = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Boss Followers")]
    [Description("Boss guards (not the Goons) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainFollower = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Goons")]
    [Description("Knight, Big Pipe and Birdeye use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainGoon = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Event Bots")]
    [Description("Event bots (crazy scav event, gifter) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire).")]
    public bool RangeBrainEvent = true;

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
    public bool ShotReactionLog = true;

    [DataMember]
    [Category("Shot Reaction/General")]
    [Name("Log Bots Aiming At You")]
    [Description("While a bot shoots at you, write one line per bot (at most every 1.5 s) to the BepInEx log: distance, the aim height above your feet, the part it chose, whether only your head was shootable, the head scatter applied, its optic, fire mode and your pose. For finding out why a shot landed where it did.")]
    public bool AimAtPlayerLog = true;

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
    public bool ShotBrainRaider = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Scavs")]
    [Description("Regular scavs, scav groups and tagged-and-cursed scavs use the fork's shot reaction.")]
    public bool ShotBrainScav = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Scav Snipers")]
    [Description("Scav snipers (marksman) use the fork's shot reaction.")]
    public bool ShotBrainScavSniper = false;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Cultists")]
    [Description("Cultists use the fork's shot reaction.")]
    public bool ShotBrainCultist = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Bosses")]
    [Description("Bosses (not the Goons) use the fork's shot reaction.")]
    public bool ShotBrainBoss = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Boss Followers")]
    [Description("Boss guards (not the Goons) use the fork's shot reaction.")]
    public bool ShotBrainFollower = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Goons")]
    [Description("Knight, Big Pipe and Birdeye use the fork's shot reaction.")]
    public bool ShotBrainGoon = true;

    [DataMember]
    [Category("Shot Reaction/Brain filter")]
    [Name("Brain: Event Bots")]
    [Description("Event bots (crazy scav event, gifter) use the fork's shot reaction.")]
    public bool ShotBrainEvent = true;

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
    public float ShortCover = 15f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Short: Fight Weight")]
    [Description("Relative weight of fighting back (after a reaction delay) in the short zone.")]
    [MinMax(0f, 100f, 1f)]
    public float ShortFight = 75f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Mid: Cover Weight")]
    [Description("Relative weight of taking cover in the mid zone.")]
    [MinMax(0f, 100f, 1f)]
    public float MidCover = 35f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Mid: Fight Weight")]
    [Description("Relative weight of fighting back in the mid zone.")]
    [MinMax(0f, 100f, 1f)]
    public float MidFight = 50f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Long: Cover Weight")]
    [Description("Relative weight of taking cover in the long zone.")]
    [MinMax(0f, 100f, 1f)]
    public float LongCover = 95f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Long: Fight Weight")]
    [Description("Relative weight of fighting back in the long zone. Only bots whose weapon reaches the shooter can pick it.")]
    [MinMax(0f, 100f, 1f)]
    public float LongFight = 2f;

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
    public float ShotFightMaxShare = 0.6f;

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
    [Name("Position Error Max")]
    [Description("The largest position error and the largest uncertainty, in meters. A hard ceiling: concealment widens the uncertainty before it, but never raises it.")]
    [MinMax(0f, 300f, 1f)]
    public float ShotPositionErrorMax = 50f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Position Error Min")]
    [Description("The smallest position error, in meters: a shot never gives the bot the shooter's exact position, even up close.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotPositionErrorMin = 4f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Prone Shooter Error Factor")]
    [Description("The position error (before the cap) is multiplied by this when the shooter lies prone: harder to place. 1 = no effect. Stacks with the suppressed factor.")]
    [MinMax(1f, 4f, 100f)]
    public float ShotProneErrorFactor = 1.3f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Suppressed Shooter Error Factor")]
    [Description("The position error (before the cap) is multiplied by this when the shooter fires a suppressed weapon. 1 = no effect. Stacks with the prone factor.")]
    [MinMax(1f, 4f, 100f)]
    public float ShotSuppressedErrorFactor = 1.15f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Hit Position Error Factor")]
    [Description("The position error is multiplied by this when the bot was actually hit (a hit says more about where the shooter is).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotHitErrorFactor = 0.85f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Fight Duration")]
    [Description("A bot that chose to fight back uses normal SAIN combat behavior at once for this many seconds, then the shooter is forgotten if the bot has not seen it.")]
    [MinMax(2f, 60f, 1f)]
    public float ShotFightDuration = 10f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Fight: Search And Fire")]
    [Description("A bot that rolled Fight against a shooter it cannot see turns to the vague shot area, fires at it and works toward it, instead of leaving it to SAIN's normal combat (which walks around until it sees the shooter). Off = a Fight is plain SAIN combat at once.")]
    public bool ShotFightSearchEnabled = true;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Fight: Search Time")]
    [Description("Seconds a Fight bot searches the vague area this way (before normal SAIN combat takes over). Seeing the shooter ends it at once.")]
    [MinMax(1f, 15f, 0.5f)]
    public float ShotFightSearchTime = 4f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Fight: Move While Searching")]
    [Description("The searching Fight bot uses SAIN's dogfight movement (strafes, backs up when it must, closes in on the vague area at a run-walk) while it fires, instead of standing still. Off = it stands.")]
    public bool ShotFightSearchMove = true;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Fight: Fire Time")]
    [Description("Seconds of that stop during which the bot fires at the vague area (the rest it just watches it). Not fired when the area is farther than the weapon reach.")]
    [MinMax(0f, 15f, 0.5f)]
    public float ShotFightFireTime = 2.5f;

    [DataMember]
    [Category("Shot Reaction/Position & timing")]
    [Name("Fight On: Fire Pause")]
    [Description("A bot that fights on from cover (suppression) fires at the vague area for Fight: Fire Time, then pauses this many seconds, and repeats, for as long as the state lasts and it has not seen the shooter. 0 = SAIN's own suppression only.")]
    [MinMax(0f, 15f, 0.5f)]
    public float ShotFightOnFirePause = 3f;

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
    [Name("Hunt Time Max")]
    [Description("A bot that chose to hunt gives up after this many seconds from the shot if it has not seen the shooter.")]
    [MinMax(5f, 180f, 1f)]
    public float ShotHuntTimeMax = 90f;

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
    [Name("Close Spot Coefficient")]
    [Description(
        "Multiplier on the time a bot needs to spot the shooter while its shot reaction is active and the shooter is within the Close Interrupt Distance. Low = the bot notices a shooter at arm's length almost at once. 1 = no bonus."
    )]
    [MinMax(0.01f, 1f, 100f)]
    public float ShotCloseSpotCoef = 0.15f;

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
    public float ShotFollowFightOn = 0.35f;

    [DataMember]
    [Category("Shot Reaction/After hiding")]
    [Name("Follow-up: Investigate")]
    [Description("Weight: after hiding, the bot goes looking for the shooter (normal search toward the vague area, stops and fights when it sees the shooter).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowInvestigate = 0.4f;

    [DataMember]
    [Category("Shot Reaction/After hiding")]
    [Name("Follow-up: Retreat")]
    [Description("Weight: after hiding, the bot relocates away from the shot area (see the Relocate settings). Ignored if Relocate is off.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowRetreat = 0.25f;

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

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Point Blank Distance")]
    [Description("Hit or shot at from this close (meters) the bot fights back with the Point Blank Fight Share chance, with no concealment effect. Hard stop, no fade. 0 = off.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotPointBlankDistance = 10f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Point Blank Fight Share")]
    [Description("Chance of fighting back inside the Point Blank Distance. 95% leaves a little randomness. The rest is cover.")]
    [MinMax(0.3f, 1f, 100f)]
    public float ShotPointBlankFightShare = 0.95f;

    [DataMember]
    [Category("Shot Reaction/Far range")]
    [Name("Far Range Fight Goes To Cover First")]
    [Description("A Fight roll beyond the Far Range Start Distance may turn into Cover followed by a (boosted) Fight On: the bot sprints to cover and fires at the vague area from there, instead of turning around and blind firing at once. The chance ramps with distance (see the other Far Range settings). Off = Fight rolls always fight at once.")]
    public bool ShotFarFightViaCover = true;

    [DataMember]
    [Category("Shot Reaction/Far range")]
    [Name("Far Range Start Distance")]
    [Description("Meters. From this distance on, a Fight roll can turn into Cover then Fight On, with a chance that starts at 0 here. Lower it to start earlier. 0 = off.")]
    [MinMax(0f, 400f, 1f)]
    public float ShotFarStartDistance = 80f;

    [DataMember]
    [Category("Shot Reaction/Far range")]
    [Name("Far Range Full Distance")]
    [Description("Meters. From this distance on the chance is at its maximum (Far Range Max Chance). Move it closer to the start distance to make the effect ramp up faster.")]
    [MinMax(20f, 500f, 1f)]
    public float ShotFarFullDistance = 120f;

    [DataMember]
    [Category("Shot Reaction/Far range")]
    [Name("Far Range Max Chance")]
    [Description("The chance that a Fight roll turns into Cover then Fight On at the full distance and beyond. 85% leaves a rare true instant Fight.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFarMaxConvert = 0.85f;

    [DataMember]
    [Category("Shot Reaction/Far range")]
    [Name("Far Range Curve")]
    [Description("Shape of the ramp between the start and the full distance. 1 = straight line, 2 = rises fast at first then flattens (chance at the halfway point is 75% of the maximum), 3 = faster still. Below 1 it starts slowly.")]
    [MinMax(0.3f, 5f, 10f)]
    public float ShotFarCurve = 2f;

    [DataMember]
    [Category("Shot Reaction/Far range")]
    [Name("Far Range Fight On Boost")]
    [Description("A bot whose Fight roll turned into Cover picks its follow-up like any hiding bot (personality, injury, reach limit, squad), but with the Fight On weight multiplied by this. 1 = no boost.")]
    [MinMax(1f, 20f, 10f)]
    public float ShotFarFightOnBoost = 4f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Close Interrupt Distance")]
    [Description("A shooter this close (meters), shooting or not, always makes the bot fight: it interrupts a hide, hold, investigation or retreat already in effect. 0 = off.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotCloseInterruptDistance = 5f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Other Enemy Interrupt Distance")]
    [Description("Any enemy other than the shooter (a scav, another PMC, a Rogue ...) that the bot can see within this distance (meters), or that hits it, ends the reaction in effect (hide, hold, investigation, retreat): normal SAIN combat takes over and deals with that enemy. 0 = off.")]
    [MinMax(0f, 50f, 1f)]
    public float ShotOtherEnemyInterruptDistance = 15f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Cornered Hits")]
    [Description("A bot that hides or retreats and takes this many hits within the Cornered Window (from the Cornered Distance or closer) stops and fights. 0 = off.")]
    [MinMax(0f, 10f, 1f)]
    public float ShotCorneredHits = 3f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Cornered Window")]
    [Description("Seconds in which the Cornered Hits have to land.")]
    [MinMax(1f, 30f, 1f)]
    public float ShotCorneredWindow = 5f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Cornered Distance")]
    [Description("Hits from farther than this (meters) do not count as cornering: the bot keeps running for cover.")]
    [MinMax(5f, 150f, 1f)]
    public float ShotCorneredDistance = 30f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Close Range (share of reach)")]
    [Description("Close range is this share of the bot weapon reach, clamped by the min and max below. Close range: no cover hops when shot at in cover, and a hiding bot may fire back.")]
    [MinMax(0.05f, 1f, 100f)]
    public float ShotCloseRangeReachFraction = 0.25f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Close Range Minimum")]
    [Description("Close range is never smaller than this many meters.")]
    [MinMax(0f, 50f, 1f)]
    public float ShotCloseRangeMin = 10f;

    [DataMember]
    [Category("Shot Reaction/Point blank & close range")]
    [Name("Close Range Maximum")]
    [Description("Close range is never larger than this many meters.")]
    [MinMax(5f, 100f, 1f)]
    public float ShotCloseRangeMax = 25f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("No Cover Grace Time")]
    [Description("Seconds the bot waits for the cover finder before deciding fight or flight when it has no cover.")]
    [MinMax(0f, 3f, 10f)]
    public float ShotNoCoverGrace = 0.8f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Base Fight Score")]
    [Description("Starting score of the fight-or-flight decision. Positive = fights more. The fight chance is 1 / (1 + e^-score).")]
    [MinMax(-5f, 5f, 10f)]
    public float ShotNoCoverBias = -0.5f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Close Range Bonus")]
    [Description("Added to the fight score when the shooter is within close range.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverCloseBonus = 1.5f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Inside Half Reach Bonus")]
    [Description("Added when the shooter is within half of the weapon reach.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverNearBonus = 0.7f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Inside Reach Bonus")]
    [Description("Added when the shooter is within the weapon reach (beyond half).")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverReachBonus = 0.3f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Just Out Of Reach Penalty")]
    [Description("Subtracted when the shooter is just beyond the reach (up to 1.25x).")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverJustOutPenalty = 1f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Far Out Of Reach Penalty")]
    [Description("Subtracted when the shooter is far beyond the reach.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverFarPenalty = 2.5f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Personality Weight")]
    [Description("Added for aggressive personalities, subtracted for timid ones.")]
    [MinMax(0f, 5f, 10f)]
    public float ShotNoCoverPersonality = 1f;

    [DataMember]
    [Category("Shot Reaction/No cover (fight or flight)")]
    [Name("Hurt Penalty")]
    [Description("Subtracted when the bot is badly injured.")]
    [MinMax(0f, 5f, 10f)]
    public float ShotNoCoverHurtPenalty = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Cover quality (bushes)")]
    [Name("Bush Path Penalty")]
    [Description("A bush counts as this many meters farther away when a bot picks cover, so solid cover wins unless a bush is that much closer. 0 = bushes are as good as walls.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotFoliagePenalty = 30f;

    [DataMember]
    [Category("Shot Reaction/Cover quality")]
    [Name("Wide Cover Search")]
    [Description("While a bot is in a shot reaction (hiding, retreating, investigating) its cover search looks further around and further up and down, so a bot on a roof also sees cover inside and below the building. Off = the normal search box (70 x 70 m, 5 m up and down).")]
    public bool ShotCoverWideSearch = true;

    [DataMember]
    [Category("Shot Reaction/Cover quality")]
    [Name("Cover Search Radius")]
    [Description("Half width (meters) of the cover search box during a shot reaction (normal bots: 35).")]
    [MinMax(20f, 120f, 1f)]
    public float ShotCoverSearchRadius = 60f;

    [DataMember]
    [Category("Shot Reaction/Cover quality")]
    [Name("Cover Search Height")]
    [Description("Half height (meters) of the cover search box during a shot reaction (normal bots: 5): how far up and down it looks for cover.")]
    [MinMax(5f, 40f, 1f)]
    public float ShotCoverSearchHeight = 15f;

    [DataMember]
    [Category("Shot Reaction/Cover quality")]
    [Name("Thin Cover Penalty")]
    [Description("Cover whose collider name says thin, plastic, glass, cardboard or cloth (a rifle round goes through it) counts as this many meters farther away during a shot reaction, so a solid object a bit further off wins. 0 = thin cover is as good as a wall.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotCoverThinPenalty = 25f;

    [DataMember]
    [Category("Shot Reaction/Cover quality")]
    [Name("Cover Hold Time")]
    [Description("After a bot reaches its cover during a shot reaction it keeps it at least this many seconds, even when the next shot moves the guessed shooter position and marks the cover as bad (no running around between props). 0 = off.")]
    [MinMax(0f, 15f, 10f)]
    public float ShotCoverHold = 4f;

    [DataMember]
    [Category("Shot Reaction/Cover quality")]
    [Name("Cover From The Shot Area")]
    [Description("The cover finder only searches while the bot has a target with a known position. A bot that was only alerted (a Rogue told about a shot, a bot that lost its target after a retreat) has none, found no cover at all and fled blindly. On = during a shot reaction the finder uses the rough shot area as the threat position when the bot has no known position. Off = vanilla finder.")]
    public bool ShotCoverFromArea = true;

    [DataMember]
    [Category("Shot Reaction/Cover quality (bushes)")]
    [Name("Bush Fade Start Distance")]
    [Description("A bush closer than this to the shot area keeps the full penalty. Beyond it, and out of the shot area line of sight, the penalty fades.")]
    [MinMax(0f, 150f, 1f)]
    public float ShotFoliageFadeStart = 30f;

    [DataMember]
    [Category("Shot Reaction/Cover quality (bushes)")]
    [Name("Bush Fade End Distance")]
    [Description("A bush this far (or farther) from the shot area and out of its line of sight has no penalty: a good ambush spot.")]
    [MinMax(10f, 300f, 1f)]
    public float ShotFoliageFadeEnd = 90f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Route: Direct Weight")]
    [Description("Weight of the straight run to the retreat point.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteDirect = 25f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Route: Bend Weight")]
    [Description("Weight of a run with one sideways waypoint.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteBend = 35f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Route: Zig-Zag Weight")]
    [Description("Weight of a run with two waypoints on alternating sides.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteZigZag = 20f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Route: Cover First Weight")]
    [Description("Weight of a run that first goes to the nearest point the shot area cannot see.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteCoverFirst = 20f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Exposed Line Limit")]
    [Description("When more than this share of the straight line is visible from the shot area, the Direct weight drops to 5 and Cover First and Zig-Zag gain weight.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRouteExposureLimit = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Waypoint Offset Min")]
    [Description("Smallest sideways offset (meters) of a bend or zig-zag waypoint.")]
    [MinMax(2f, 40f, 1f)]
    public float ShotRouteOffsetMin = 8f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Waypoint Offset Max")]
    [Description("Largest sideways offset (meters) of a bend or zig-zag waypoint.")]
    [MinMax(4f, 60f, 1f)]
    public float ShotRouteOffsetMax = 20f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Retreat Squad Weight")]
    [Description("Score bonus for retreat points close to a squadmate (full bonus next to a mate, none at the Retreat Squad Range). A retreating bot falls back toward its group. 0 = ignore the squad.")]
    [MinMax(0f, 3f, 100f)]
    public float ShotRetreatSquadWeight = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Retreat route")]
    [Name("Retreat Squad Range")]
    [Description("Distance (meters) from the nearest squadmate beyond which a retreat point gets no squad bonus.")]
    [MinMax(10f, 200f, 1f)]
    public float ShotRetreatSquadRange = 60f;

    [DataMember]
    [Category("Shot Reaction/Squad")]
    [Name("Squad Roles Enabled")]
    [Description("The follow-up pick after a hide takes squadmates into account: a mate that fights (Fight, Fight On) makes the others lean to investigate (flank), a mate that investigates makes the others lean to fight on from cover (cover fire). Off = every bot picks on its own.")]
    public bool ShotSquadRolesEnabled = true;

    [DataMember]
    [Category("Shot Reaction/Squad")]
    [Name("Squad Role Range")]
    [Description("Squadmates farther than this (meters) do not count for the squad roles and the squad alert.")]
    [MinMax(10f, 150f, 1f)]
    public float ShotSquadRoleRange = 40f;

    [DataMember]
    [Category("Shot Reaction/Squad")]
    [Name("Squad Role Strength")]
    [Description("Added to the lean per qualifying squadmate (0.4 = 40% more weight on the complementary pick), capped at +100%.")]
    [MinMax(0f, 2f, 100f)]
    public float ShotSquadRoleStrength = 0.4f;

    [DataMember]
    [Category("Shot Reaction/Squad")]
    [Name("Squad Alert Enabled")]
    [Description("When a bot is hit by a shooter it cannot see, squadmates within the Squad Role Range that know the shooter and are not already reacting get the vague shot area and roll their own reaction as for a close bullet. Off = only the hit bot reacts (SAIN still shares the heard position).")]
    public bool ShotSquadAlertEnabled = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Gun Abandon Enabled")]
    [Description("A bot on (or running to) a mounted gun that is shot at by an unseen shooter checks the direction of the shot. Shooter inside the gun's firing sector: the bot stays and answers from the gun. Shooter outside the sector: it leaves the gun and reacts as any other bot (Cover or Fight roll). Off = vanilla behaviour (the gun layer outranks the reaction and the bot stays).")]
    public bool ShotStationaryEnabled = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Gun Hold Off Time")]
    [Description("Seconds a bot that left its mounted gun because of a shot is kept off it (vanilla would send it back after 45-60 s).")]
    [MinMax(0f, 1800f, 1f)]
    public float ShotStationaryHold = 300f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Gun Answer Grace")]
    [Description("A gunner whose gun covers the shot stays on it only while it can answer. If it has not pulled the trigger (suppression counts) for this many seconds when the next hit, close bullet or alert reaches it, it leaves the post like a gunner the shot came outside of the arc for, and takes cover. 0 = it always stays when the shot is inside the arc.")]
    [MinMax(0f, 30f, 10f)]
    public float ShotStationaryAnswerGrace = 6f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Close Bullet Distance")]
    [Description("A bullet that passes within this distance (meters) of a Rogue on a mounted gun counts as a close bullet for the shot reaction (normal bots: Max Under Fire Distance, about 2 m). 0 = same as other bots.")]
    [MinMax(0f, 50f, 1f)]
    public float ShotMountedUnderFireDistance = 15f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Impact Range")]
    [Description("A bullet of the player that lands within this distance (meters) of a Rogue on a mounted gun counts as a close bullet too (the Rogue sees where the shot landed). 0 = off.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotMountedImpactRange = 25f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Suppression")]
    [Description("A Rogue on a mounted gun (grenade launcher or machine gun) that knows only a rough shot area (a hit on itself, a Rogue alert) and cannot see the player fires suppression bursts at that area, not at the player. Random: a chance roll, random burst length, random pauses, a jittering target. Off = it fires only at a player it knows.")]
    public bool MountedAreaSuppress = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Chance")]
    [Description("Chance that an eligible gunner starts a suppression burst at each check (checks come every 1 to 1.5 s). A failed roll waits 2 to 5 s before the next.")]
    [MinMax(0f, 1f, 100f)]
    public float MountedAreaSuppressChance = 0.6f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Age")]
    [Description("The rough shot area counts only this many seconds after the last shot, hit or alert that refreshed it (seconds).")]
    [MinMax(5f, 120f, 1f)]
    public float MountedAreaSuppressAreaAge = 30f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Error")]
    [Description("Radius (meters) of the jitter around the rough area that the gun aims at, re-rolled every second. The area itself already has the shot position error on top.")]
    [MinMax(0f, 60f, 1f)]
    public float MountedAreaSuppressError = 12f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Grenade Burst Min")]
    [Description("Shortest suppression burst of a grenade launcher (seconds).")]
    [MinMax(1f, 30f, 10f)]
    public float MountedGrenadeBurstMin = 1f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Grenade Burst Max")]
    [Description("Longest suppression burst of a grenade launcher (seconds).")]
    [MinMax(1f, 30f, 10f)]
    public float MountedGrenadeBurstMax = 3f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted MG Burst Min")]
    [Description("Shortest suppression burst of a mounted machine gun (seconds).")]
    [MinMax(0.5f, 20f, 10f)]
    public float MountedMgBurstMin = 1.5f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted MG Burst Max")]
    [Description("Longest suppression burst of a mounted machine gun (seconds).")]
    [MinMax(0.5f, 20f, 10f)]
    public float MountedMgBurstMax = 4f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Pause Min")]
    [Description("Shortest pause between two suppression bursts of the same gunner (seconds).")]
    [MinMax(0f, 120f, 10f)]
    public float MountedAreaCooldownMin = 6f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Pause Max")]
    [Description("Longest pause between two suppression bursts of the same gunner (seconds).")]
    [MinMax(0f, 120f, 10f)]
    public float MountedAreaCooldownMax = 20f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Start Delay Min")]
    [Description("After a quiet spell (45 s without suppression) a gunner waits at least this long (seconds) after it first has a rough shot area before it starts suppressing, so it does not answer your first shot at once.")]
    [MinMax(0f, 60f, 10f)]
    public float MountedAreaStartDelayMin = 4f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Area Start Delay Max")]
    [Description("Longest wait before the first suppression burst after a quiet spell (seconds).")]
    [MinMax(0f, 60f, 10f)]
    public float MountedAreaStartDelayMax = 8f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Grenade Pause Min")]
    [Description("Shortest pause between two suppression bursts of a grenade launcher (seconds). Machine guns use Mounted Area Pause.")]
    [MinMax(0f, 120f, 10f)]
    public float MountedGrenadePauseMin = 20f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Grenade Pause Max")]
    [Description("Longest pause between two suppression bursts of a grenade launcher (seconds).")]
    [MinMax(0f, 120f, 10f)]
    public float MountedGrenadePauseMax = 45f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Grenade Max Range")]
    [Description("A grenade launcher does not area-suppress, and does not count as able to answer, a rough area farther than this (meters) when it does not see its target: it takes cover instead. It still fires at a target it sees. 0 = no limit.")]
    [MinMax(0f, 2000f, 1f)]
    public float MountedGrenadeMaxRange = 0f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty High")]
    [Description("The uncertainty (meters, uncapped: distance x error per meter x hit x concealment x weather x light) at which the uncertainty effects below are at full strength. 0 = off.")]
    [MinMax(0f, 500f, 1f)]
    public float ShotUncertaintyHigh = 50f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty Fight Floor")]
    [Description("At full uncertainty the Fight share of the first reaction roll is multiplied by this (1 = no effect): a bot that cannot tell where the shot came from fights less.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotUncertaintyFightMin = 0.3f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty Investigate Floor")]
    [Description("At full uncertainty the Investigate weight of the follow-up pick after hiding is multiplied by this (1 = no effect): a bot that cannot tell where the shot came from goes looking less.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotUncertaintyInvestigateMin = 0.4f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Grenade Launcher Max Uncertainty")]
    [Description("A Rogue on a grenade launcher area-suppresses only while the uncertainty of its rough shot area is at most this (meters). Above it, and without seeing its target, it cannot pinpoint the shooter: it leaves the post and takes cover. 0 = no limit.")]
    [MinMax(0f, 500f, 1f)]
    public float MountedGrenadeMaxUncertainty = 20f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Machine Gun Max Uncertainty")]
    [Description("The same limit for a Rogue on a mounted machine gun. 0 = no limit.")]
    [MinMax(0f, 500f, 1f)]
    public float MountedMgMaxUncertainty = 30f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Bush Shooter Error Factor")]
    [Description("The uncertainty of a guessed shot position is multiplied by this when the shooter fires from inside a bush (foliage around the shooter): a common ambush, harder to place. 1 = no effect. Stacks with prone and suppressed.")]
    [MinMax(1f, 4f, 100f)]
    public float ShotBushErrorFactor = 1.3f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Tracer Error Factor")]
    [Description("The uncertainty is multiplied by this for a short while after a tracer round of the shooter flew past or landed near the bot: a tracer shows where it came from. Below 1 makes the guess better. 1 = no effect.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotTracerErrorFactor = 0.7f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty At Sight")]
    [Description("The uncertainty (meters) of a miss from a standing, loud shooter at exactly the bot's view distance (its current effective view distance for that shooter: weather, night, goggles, lights, movement, flare and gear are already in it). Closer it falls in proportion, down to 0 at the bot.")]
    [MinMax(1f, 60f, 10f)]
    public float ShotUncertaintyAtSight = 20f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty Beyond Sight")]
    [Description("Extra uncertainty (meters) for every further view distance the shooter is beyond the bot's sight: 15 = 35 m at twice the view distance, 50 m at three times. The total is capped by Position Error Max.")]
    [MinMax(0f, 60f, 10f)]
    public float ShotUncertaintyBeyondSight = 15f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty Min Sight")]
    [Description("The view distance (meters) is never taken as less than this when the uncertainty is computed (a very dark night would otherwise blow it up).")]
    [MinMax(5f, 200f, 1f)]
    public float ShotUncertaintyMinSight = 30f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Fight Fire Max Uncertainty")]
    [Description("Fight On and the fight search fire at the rough shot area only while its uncertainty (meters) is at most this. Above it the bot holds fire. 0 = no limit.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotFightFireMaxUncertainty = 30f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Grenade Max Uncertainty")]
    [Description("A bot throws a grenade at the rough shot area only while its uncertainty (meters) is at most this (a grenade's splash is only a few meters wide). 0 = no limit.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotNadeMaxUncertainty = 20f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty Fire Time Floor")]
    [Description("At full uncertainty (Uncertainty High) the burst length of Fight On is multiplied by this: a bot that cannot tell where the shooter is fires shorter bursts.")]
    [MinMax(0.05f, 1f, 100f)]
    public float ShotUncertaintyFireTimeMin = 0.3f;

    [DataMember]
    [Category("Shot Reaction/Uncertainty")]
    [Name("Uncertainty Pause Factor")]
    [Description("At full uncertainty (Uncertainty High) the pause between Fight On bursts is multiplied by this.")]
    [MinMax(1f, 10f, 10f)]
    public float ShotUncertaintyPauseMax = 3f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Alert Enabled")]
    [Description("When a Rogue is hit by, or a bullet passes close to it from, a shooter it cannot see, the other Rogues within the Rogue Alert Range get the vague shot area and take Cover (staggered by up to Rogue Alert Delay). A Rogue on a mounted gun whose sector covers that area stays on the gun. Needs Mounted Gun Abandon Enabled for the gun part.")]
    public bool ShotRogueAlertEnabled = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Alert Range")]
    [Description("Rogues farther than this (meters) from the hit Rogue are not alerted. 0 = every Rogue on the map (the delay grows with the distance, see Rogue Alert Delay Per Meter). Also limits the vanilla hit broadcast.")]
    [MinMax(0f, 2000f, 1f)]
    public float ShotRogueAlertRange = 0f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Alert Delay Per Meter")]
    [Description("Extra seconds per meter between the hit Rogue and an alerted Rogue (0.01 = 5 s over 500 m), added to the random 0 to Rogue Alert Delay seconds.")]
    [MinMax(0f, 0.1f, 1000f)]
    public float ShotRogueAlertDelayPerMeter = 0.01f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Alert Delay Min")]
    [Description("Minimum seconds before an alerted Rogue reacts, even the nearest one (reaction time). The alert delay is a random value from this to Rogue Alert Delay, plus the per-meter delay.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotRogueAlertDelayMin = 1f;

    [DataMember]
    [Category("Shot Reaction/Squad")]
    [Name("Squad Alert Delay Min")]
    [Description("Minimum seconds before a squadmate reacts to a squad alert (a hit or kill of a squadmate), so it does not snap into the reaction at once.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotSquadAlertDelayMin = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Squad")]
    [Name("Squad Alert Delay Max")]
    [Description("Maximum seconds before a squadmate reacts to a squad alert.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotSquadAlertDelayMax = 2f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Alert Chance")]
    [Description("Chance per Rogue within the Rogue Alert Range that it takes the alert (rolled once per Rogue per hit). 0 = nobody, 1 = every Rogue in range.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRogueAlertChance = 1f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Alert Delay")]
    [Description("Each alerted Rogue reacts after a random delay from 0 to this many seconds, so they do not all move on the same frame.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotRogueAlertDelayMax = 4f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogues: No Zone Magic")]
    [Description("Vanilla tells every Rogue in an attack zone (water treatment plant) the exact position of any player who enters the zone and marks the player as visible to them, however far away and hidden. On = that is removed: Rogues must find the player by sight and sound. Off = vanilla.")]
    public bool RogueNoZoneMagic = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogues: Limited Hit Broadcast")]
    [Description("Vanilla makes every Rogue on the map take the shooter of any hit Rogue as an enemy, whatever the distance. On = only Rogues within the Rogue Alert Range of the hit Rogue, each with the Rogue Alert Chance, take it (the Rogue itself always does). Off = vanilla.")]
    public bool RogueLimitedBroadcast = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Guns: Known Position Only")]
    [Description("Vanilla mounted guns (machine guns, grenade launchers) aim and suppress at the player's TRUE live position even when the Rogue has not seen the player. On = they use the last position the Rogue actually knew, plus an error (Mounted Aim Error), and do not start suppression without knowing the player. Off = vanilla.")]
    public bool RogueMountedKnownPosition = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Guns: Aim Error")]
    [Description("Error radius (meters) around the last known position that a mounted gun aims or suppresses at (re-rolled every 3 s).")]
    [MinMax(0f, 50f, 1f)]
    public float RogueMountedAimError = 8f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Mounted Guns: Spot Speed In Zone")]
    [Description("Vision speed multiplier for a Rogue on a mounted gun against a player inside the Rogue attack zone: 0.5 = twice as fast to spot, 1 = off. It only speeds up spotting, the player's position is not given away.")]
    [MinMax(0.1f, 1f, 100f)]
    public float RogueMountedSpotCoef = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Reports: Delayed")]
    [Description("Vanilla gives every Rogue a spotting Rogue's report of the player (exact position) at once, at any distance. On = each Rogue gets it after a delay (min to max seconds plus travel time per meter), and only inside the report range.")]
    public bool RogueReportDelayed = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Reports: Delay Min")]
    [Description("Minimum seconds before a Rogue receives another Rogue's enemy report.")]
    [MinMax(0f, 30f, 10f)]
    public float RogueReportDelayMin = 2f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Reports: Delay Max")]
    [Description("Maximum base seconds before a Rogue receives another Rogue's enemy report.")]
    [MinMax(0f, 60f, 10f)]
    public float RogueReportDelayMax = 6f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Reports: Delay Per Meter")]
    [Description("Extra seconds per meter between the reporting Rogue and the receiving Rogue (0.01 = 5 s over 500 m).")]
    [MinMax(0f, 0.1f, 1000f)]
    public float RogueReportDelayPerMeter = 0.01f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Rogue Reports: Range")]
    [Description("Rogues farther than this (meters) from the reporting Rogue never receive its report. 0 = the whole map.")]
    [MinMax(0f, 2000f, 1f)]
    public float RogueReportRange = 0f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Grenade Launcher: Scatter")]
    [Description("Mounted grenade launchers (AGS) get extra aim error that grows with the distance to the target, so a launcher far away lands its grenades well off the player. Machine guns are not affected. Off = vanilla.")]
    public bool GrenadeLauncherScatter = true;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Grenade Launcher: Scatter Start Distance")]
    [Description("Distance (meters) from which the grenade launcher scatter starts to grow.")]
    [MinMax(0f, 500f, 1f)]
    public float GrenadeScatterStart = 100f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Grenade Launcher: Scatter Per Meter")]
    [Description("Scatter radius (meters at the target) added per meter beyond the start distance (0.05 = 25 m of radius at 600 m with a 100 m start).")]
    [MinMax(0f, 0.5f, 1000f)]
    public float GrenadeScatterPerMeter = 0.05f;

    [DataMember]
    [Category("Shot Reaction/Mounted guns")]
    [Name("Grenade Launcher: Scatter Max")]
    [Description("Largest scatter radius (meters at the target).")]
    [MinMax(0f, 200f, 1f)]
    public float GrenadeScatterMax = 60f;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenades Enabled")]
    [Description("A bot that holds cover after a hide may throw a grenade at the vague shot area first (before it retreats, fights on from cover, investigates, or starts an ambush). Uses SAIN's own throw logic and its safety checks (not while running, friendlies close, range, needs a grenade). Off = the reaction never throws.")]
    public bool ShotGrenadeEnabled = true;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenade Chance: Retreat")]
    [Description("Chance of a grenade at the end of a hide that continues as a retreat (cover, nade, run).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceRetreat = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenade Chance: Fight On")]
    [Description("Chance of a grenade at the end of a hide that continues as Fight On (suppression from cover).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceFightOn = 0.35f;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenade Chance: Investigate")]
    [Description("Chance of a grenade at the end of a hide that continues as an investigation.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceInvestigate = 0.3f;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenade Chance: Ambush")]
    [Description("Chance of a grenade when an investigating bot has no closer cover and settles into its ambush.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceAmbush = 0.3f;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenade Min Distance")]
    [Description("No grenade at a vague area closer than this (meters).")]
    [MinMax(5f, 60f, 1f)]
    public float ShotGrenadeMinDistance = 20f;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenade Max Distance")]
    [Description("No grenade at a vague area farther than this (meters).")]
    [MinMax(20f, 100f, 1f)]
    public float ShotGrenadeMaxDistance = 60f;

    [DataMember]
    [Category("Shot Reaction/Grenades")]
    [Name("Grenade Cooldown")]
    [Description("Seconds before the same bot may throw again from a shot reaction.")]
    [MinMax(5f, 300f, 1f)]
    public float ShotGrenadeCooldown = 45f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Safety: Quiet Time")]
    [Description("Seconds without being shot at or seeing the shooter until the quiet part of the safety level is full.")]
    [MinMax(5f, 180f, 1f)]
    public float ShotSafetyQuietTime = 40f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Safety: Distance Start")]
    [Description("Distance from the shot area where the distance part of the safety level starts to count.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotSafetyDistStart = 20f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Safety: Distance Full")]
    [Description("Distance from the shot area where the distance part of the safety level is full.")]
    [MinMax(20f, 300f, 1f)]
    public float ShotSafetyDistFull = 100f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Safe Enough: Min")]
    [Description("After a retreat the bot holds until its safety level reaches a random value between Min and Max (aggressive bots lower, timid bots higher).")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotSafetyThresholdMin = 0.6f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Safe Enough: Max")]
    [Description("Upper end of the random safe-enough level.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotSafetyThresholdMax = 0.9f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Hold After Retreat: Minimum")]
    [Description("The bot holds at least this many seconds after a retreat, whatever its safety level.")]
    [MinMax(0f, 60f, 1f)]
    public float ShotPostRetreatMinHold = 10f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Hold After Retreat: Longest (min)")]
    [Description("Upper limit of the hold after a retreat is a random value between this and the next setting (scaled by personality).")]
    [MinMax(5f, 180f, 1f)]
    public float ShotPostRetreatHoldMin = 30f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Hold After Retreat: Longest (max)")]
    [Description("Upper end of the longest hold after a retreat.")]
    [MinMax(5f, 300f, 1f)]
    public float ShotPostRetreatHoldMax = 60f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Stay Mobile Below Safety")]
    [Description("While the safety level is below this, a holding bot moves to another cover every few seconds instead of sitting still.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotMobileSafety = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Mobile Hop Interval: Min")]
    [Description("Shortest time between cover moves while the bot does not feel safe.")]
    [MinMax(2f, 60f, 1f)]
    public float ShotMobileHopMin = 6f;

    [DataMember]
    [Category("Shot Reaction/Safety & hold")]
    [Name("Mobile Hop Interval: Max")]
    [Description("Longest time between cover moves while the bot does not feel safe.")]
    [MinMax(2f, 60f, 1f)]
    public float ShotMobileHopMax = 12f;

    [DataMember]
    [Category("Shot Reaction/Shot again")]
    [Name("Another Retreat: Hit Chance")]
    [Description("Chance that a hit during a retreat or the hold after it starts another retreat (personality scaled).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatHitChance = 0.6f;

    [DataMember]
    [Category("Shot Reaction/Shot again")]
    [Name("Another Retreat: Bullet Chance")]
    [Description("The same for a bullet passing close.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatBulletChance = 0.25f;

    [DataMember]
    [Category("Shot Reaction/Shot again")]
    [Name("Startled Bonus")]
    [Description("Added to the chance when the bot had just felt safe (safety at or above the startled level).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatStartledBonus = 0.3f;

    [DataMember]
    [Category("Shot Reaction/Shot again")]
    [Name("Startled Safety Level")]
    [Description("A bot whose safety level was at least this when shot counts as startled (and retreats 25 percent farther).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatStartledSafety = 0.6f;

    [DataMember]
    [Category("Shot Reaction/Shot again")]
    [Name("Another Retreat: Max Count")]
    [Description("Most extra retreats per reaction chain.")]
    [MinMax(0f, 10f, 1f)]
    public int ShotRepeatMax = 2;

    [DataMember]
    [Category("Shot Reaction/Shot again")]
    [Name("Another Retreat: Interval")]
    [Description("Minimum seconds between extra retreats.")]
    [MinMax(0f, 60f, 1f)]
    public float ShotRepeatInterval = 8f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Move On Hit")]
    [Description("Chance that a bot hit while sheltering moves to another cover (personality scaled). Not at close range, never while fighting.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSelfPreserveHit = 0.9f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Move On Bullet")]
    [Description("The same for a bullet passing close (a bot in a bush uses the hit chance).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSelfPreserveBullet = 0.2f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Move Interval")]
    [Description("Minimum seconds between such cover moves.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotSelfPreserveInterval = 3f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Break Investigation: Hit Chance")]
    [Description("Chance that a hit while investigating makes the bot run for cover and reassess.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotInvestigateBreakHit = 1f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Break Investigation: Bullet Chance")]
    [Description("The same for a bullet passing close.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotInvestigateBreakBullet = 0.2f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Break Investigation: Lockout")]
    [Description("After an investigation was broken by fire, no further break for this many seconds.")]
    [MinMax(0f, 120f, 1f)]
    public float ShotBreakLockout = 15f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Cover Hold: Min")]
    [Description("While investigating, the bot holds each cover at least this many seconds (personality scaled).")]
    [MinMax(0f, 30f, 1f)]
    public float ShotHopHoldMin = 3f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Cover Hold: Max")]
    [Description("The longest hold in each cover while investigating.")]
    [MinMax(0f, 60f, 1f)]
    public float ShotHopHoldMax = 7f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Ambush Hold: Min")]
    [Description("When the bot gets close to the shot area or hears the shooter it holds an ambush this many seconds at least.")]
    [MinMax(0f, 120f, 1f)]
    public float ShotAmbushMin = 20f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Ambush Hold: Max")]
    [Description("Longest ambush hold.")]
    [MinMax(0f, 240f, 1f)]
    public float ShotAmbushMax = 40f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Ambush Distance")]
    [Description("The bot stops advancing and holds when it is this close (meters) to the shot area.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotInvestigateStopDistance = 15f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Advance Without Cover Chance")]
    [Description("A bot that investigates, sits in cover with no closer cover point and is still farther than the Ambush Distance from the shot area runs toward the area (instead of holding and watching) with this chance. Aggressive personalities advance more often, timid ones less. 0 = always hold.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotAdvanceNoCoverChance = 0.6f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Advance Without Cover Time")]
    [Description("Longest time (seconds) of that run. After it the bot holds and watches wherever it is.")]
    [MinMax(3f, 40f, 1f)]
    public float ShotAdvanceNoCoverTime = 12f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Sprinting Search After Hunt")]
    [Description("After a hunt runs out with the shooter unseen, the bot searches at a run for this many seconds. 0 = off.")]
    [MinMax(0f, 300f, 1f)]
    public float ShotReturnWindow = 60f;

    [DataMember]
    [Category("Shot Reaction/Bot state (running, facing)")]
    [Name("Running Penalty Fade Distance")]
    [Description("A running bot leans toward cover (Cover x1.3, Fight x0.5). That effect is full at this distance (meters) and fades to nothing at the Point Blank Distance, so a running bot hit from close range does not just flee.")]
    [MinMax(10f, 100f, 1f)]
    public float ShotRunningFadeDistance = 30f;

    [DataMember]
    [Category("Shot Reaction/Bot state (running, facing)")]
    [Name("Facing Angle")]
    [Description("A bot whose look direction is within this many degrees of the shot direction counts as facing it.")]
    [MinMax(5f, 90f, 1f)]
    public float ShotFacingAngle = 30f;

    [DataMember]
    [Category("Shot Reaction/Bot state (running, facing)")]
    [Name("Facing Fight Bonus")]
    [Description("Fight weight multiplier for a bot that is facing the shot. 1 = no effect.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotFacingFightBonus = 1.3f;

    [DataMember]
    [Category("Shot Reaction/Bot state (running, facing)")]
    [Name("Back Turned Angle")]
    [Description("A bot whose look direction is at least this many degrees away from the shot direction counts as having its back turned.")]
    [MinMax(90f, 180f, 1f)]
    public float ShotBackTurnedAngle = 120f;

    [DataMember]
    [Category("Shot Reaction/Bot state (running, facing)")]
    [Name("Back Turned Cover Bonus")]
    [Description("Cover weight multiplier for a bot with its back turned to the shot. 1 = no effect.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotBackTurnedCoverBonus = 1.3f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Strong Weapon Range")]
    [Description("A shooter closer than this share of the bot's effective reach counts as close for a strong weapon: the Cover share is limited to the next setting there.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotStrongWeaponRatio = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Strong Weapon Cover Limit")]
    [Description("Largest Cover share when the shooter is inside the Strong Weapon Range. The rest goes to fighting back.")]
    [MinMax(0.2f, 1f, 100f)]
    public float ShotStrongWeaponCoverMax = 0.6f;

    [DataMember]
    [Category("Shot Reaction/Weapon & reach")]
    [Name("Reference Reach")]
    [Description("Up close, every weapon is judged against this reach (meters) instead of its own, so a long-range weapon does not count as 'very close' at 18 m. The bot's own reach takes over linearly up to the full effect distance.")]
    [MinMax(20f, 300f, 1f)]
    public float ShotReferenceReach = 60f;

    [DataMember]
    [Category("Shot Reaction/Weapon & reach")]
    [Name("Reach Full Effect Distance")]
    [Description("From this distance (meters) on, the bot's own weapon reach decides the roll completely. Closer, the reference reach is blended in.")]
    [MinMax(10f, 300f, 1f)]
    public float ShotReachFullEffectDistance = 60f;

    [DataMember]
    [Category("Shot Reaction/Weapon & reach")]
    [Name("Weapon Type Effect Distance")]
    [Description("The weapon type factors below apply fully up to this distance (meters) and fade out to nothing at twice this distance.")]
    [MinMax(5f, 150f, 1f)]
    public float ShotWeaponEffectDistance = 30f;

    [DataMember]
    [Category("Shot Reaction/Weapon & reach")]
    [Name("SMG / Shotgun Fight Bonus")]
    [Description("Fight weight multiplier for submachine guns and shotguns up close. 1 = no effect.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponShortGunFightBonus = 1.3f;

    [DataMember]
    [Category("Shot Reaction/Weapon & reach")]
    [Name("Marksman / Sniper Cover Bonus")]
    [Description("Cover weight multiplier for marksman and sniper rifles up close (they are poor at close range). 1 = no effect.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponLongGunCoverBonus = 1.4f;

    [DataMember]
    [Category("Shot Reaction/Weapon & reach")]
    [Name("Full Auto Fight Bonus")]
    [Description("Fight weight multiplier for a weapon that can fire full auto, up close. 1 = no effect.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponFullAutoFightBonus = 1.15f;

    [DataMember]
    [Category("Shot Reaction/Weapon & reach")]
    [Name("Semi Auto Only Cover Bonus")]
    [Description("Cover weight multiplier for a weapon that cannot fire full auto, up close. 1 = no effect.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponSemiOnlyCoverBonus = 1.15f;

    [DataMember]
    [Category("Shot Reaction/Shot again")]
    [Name("Another Retreat: Min Time Since Retreat Start")]
    [Description("A repeat retreat can only start this many seconds after the previous retreat began, so a second hit right at the start does not restart it.")]
    [MinMax(0f, 20f, 10f)]
    public float ShotRepeatMinRetreatTime = 3f;

    [DataMember]
    [Category("Shot Reaction/Zones & weights")]
    [Name("Distance Scale")]
    [Description("Multiplies the distance (as a share of the bot's reach) before the zone weights are read. Below 1 every distance counts as shorter, so bots fight more; above 1 they hide more. Does not change point blank, close range, or the fade of Fight beyond the real reach.")]
    [MinMax(0.3f, 2f, 100f)]
    public float ShotDistanceScale = 0.8f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Hop: No Progress Time")]
    [Description("An investigation hop is abandoned when the bot has not gotten closer to its cover for this many seconds.")]
    [MinMax(1f, 30f, 10f)]
    public float ShotHopNoProgressTime = 5f;

    [DataMember]
    [Category("Shot Reaction/Investigate timing")]
    [Name("Hop: Longest Time")]
    [Description("The longest an investigation hop may take (seconds), however much progress it makes.")]
    [MinMax(10f, 120f, 1f)]
    public float ShotHopMaxTime = 40f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Rogue Radio: Delayed Call-In")]
    [Description("On = a Rogue that is hit or has a bullet pass close does not react at once: after a short wait it runs for cover and radios the others, both at the same moment. A Rogue that dies during the wait makes no call (the kill rules below apply). Also removes the vanilla instant hit broadcast. Off = react at once, vanilla broadcast limited by Rogue Alert Range and Chance.")]
    public bool RogueCallInDelayed = true;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Hit Call-In Delay Min")]
    [Description("Shortest wait (seconds) between a hit on a Rogue and its cover run and radio call.")]
    [MinMax(0f, 5f, 100f)]
    public float RogueHitCallInMin = 0.1f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Hit Call-In Delay Max")]
    [Description("Longest wait (seconds) between a hit on a Rogue and its cover run and radio call.")]
    [MinMax(0f, 5f, 100f)]
    public float RogueHitCallInMax = 1f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Bullet Call-In Delay Min")]
    [Description("Shortest wait (seconds) between a bullet passing close to a Rogue and its cover run and radio call.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueFlyByCallInMin = 1f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Bullet Call-In Delay Max")]
    [Description("Longest wait (seconds) between a bullet passing close to a Rogue and its cover run and radio call. If the bullet kills the Rogue, nothing is sent from the bullet.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueFlyByCallInMax = 2f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Kill Needs A Witness")]
    [Description("On = the death of a Rogue is only radioed if another living Rogue could see it (within that Rogue's visible distance, inside its visible angle, clear line of sight to the body), after Witness Delay. Nobody saw it: no radio until the Man-Down Delay ends, then everybody covers with no position. Off = every kill alerts all Rogues at once.")]
    public bool RogueKillNeedsWitness = true;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Witness Delay Min")]
    [Description("Shortest wait (seconds) between a witnessed kill and the radio call.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueKillWitnessDelayMin = 1f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Witness Delay Max")]
    [Description("Longest wait (seconds) between a witnessed kill and the radio call.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueKillWitnessDelayMax = 2f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Man-Down Delay Min")]
    [Description("Shortest wait (seconds) before the Rogues notice a Rogue that died unseen. Then every Rogue takes cover from the body's area (a Rogue on a mounted gun leaves it), with no position of the shooter and no suppression.")]
    [MinMax(0f, 120f, 1f)]
    public float RogueManDownDelayMin = 20f;

    [DataMember]
    [Category("Shot Reaction/Rogue radio")]
    [Name("Man-Down Delay Max")]
    [Description("Longest wait (seconds) before the Rogues notice a Rogue that died unseen. A real alert in the meantime cancels it.")]
    [MinMax(0f, 120f, 1f)]
    public float RogueManDownDelayMax = 40f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Rough Area Inner Fraction")]
    [Description("The rough area of a shot, hit or alert is a random point in a ring around the shooter: not closer than this share of the area's radius. 0.4 = a 50 m area never lands within 20 m of the shooter, so a lucky draw cannot put a searching bot on top of the player. 0 = anywhere in the disk (old behaviour).")]
    [MinMax(0f, 0.9f, 100f)]
    public float ShotAreaInnerFraction = 0.4f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Area Shrink Per Piece")]
    [Description("Every piece of information about the player's position that reaches a bot (a shot, hit, alert or report; at most one per Cooldown) makes its rough areas this much narrower (0.92 = 8% each, the radius and the uncertainty). Counts are forgotten after Area Memory seconds without any. 1 = areas never narrow.")]
    [MinMax(0.5f, 1f, 100f)]
    public float AreaShrink = 0.92f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Area Shrink Cooldown")]
    [Description("Seconds between two counted pieces of information: a full-auto burst, several alerts or reports in a few seconds count once.")]
    [MinMax(0f, 30f, 10f)]
    public float AreaShrinkCooldown = 5f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Area Floor")]
    [Description("Narrowing never takes a rough area (its radius and uncertainty) below this many meters when the shooter is at the bot's sight distance or farther; closer it is proportionally lower (half the sight distance = half the floor), so close fights keep their tight areas. An area that was smaller to begin with is left as it was. A mounted gun fires at it once its uncertainty is within the gun's limit (machine gun 30 m, grenade launcher 20 m).")]
    [MinMax(0f, 100f, 1f)]
    public float AreaFloorOuter = 20f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Area Memory")]
    [Description("Seconds without any information about the player after which a bot's areas start wide again.")]
    [MinMax(5f, 600f, 1f)]
    public float AreaMemory = 60f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Reports: Rough Points")]
    [Description("On = a report from another Rogue gives the receiver a point in a ring around the player, not the exact position: as rough as a shot from the receiver's own distance would be (concealment included), narrowed like every rough area. Off = the exact position, delayed (Report Delay).")]
    public bool RogueReportRough = true;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Report Minimum Interval")]
    [Description("A Rogue takes at most one report from the others per this many seconds; the vanilla report fires many times per second while a Rogue sees the player (hundreds of calls per second on the map). The rest are dropped before any work is done. 0 = no limit.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueReportMinInterval = 1f;

    [DataMember]
    [Category("Shot Reaction/Diagnostics")]
    [Name("Performance Log")]
    [Description("Every 10 seconds one log line with how much time the shot reaction code took (average ms per second, worst call, worst frame, per part) and how many vanilla report calls came in and were taken.")]
    public bool ShotPerfLog = true;

    [DataMember]
    [Category("Shot Reaction/Cover")]
    [Name("Cover: Low Height")]
    [Description("During a shot reaction a cover lower than this (meters) leaves the head exposed: it counts as Low Penalty meters farther away. 0 = off.")]
    [MinMax(0f, 2f, 100f)]
    public float ShotCoverLowHeight = 1f;

    [DataMember]
    [Category("Shot Reaction/Cover")]
    [Name("Cover: Low Penalty")]
    [Description("Meters added to the path length of a low cover (see Low Height) when a bot in a shot reaction picks its cover.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotCoverLowPenalty = 15f;

    [DataMember]
    [Category("Shot Reaction/Cover")]
    [Name("Cover: Max Bots Per Point")]
    [Description("A cover point that already has this many other bots in a shot reaction at it (or on their way to it, within Crowd Radius) is not offered to another one: no more piling into one spot. When every candidate is taken the bot's no-cover decision applies (fight or flee). 0 = no limit.")]
    [MinMax(0f, 10f, 1f)]
    public float ShotCoverMaxPerPoint = 2f;

    [DataMember]
    [Category("Shot Reaction/Cover")]
    [Name("Cover: Crowd Radius")]
    [Description("Radius (meters) around a cover point inside which other bots' cover counts as the same spot.")]
    [MinMax(1f, 30f, 1f)]
    public float ShotCoverCrowdRadius = 6f;

    [DataMember]
    [Category("Shot Reaction/Cover")]
    [Name("Cover Search Interval In Cover")]
    [Description("Seconds between two cover searches for a bot in a shot reaction that already holds a cover (the normal interval is 0.1 s).")]
    [MinMax(0.1f, 5f, 10f)]
    public float ShotCoverHoldFindInterval = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Cover")]
    [Name("Cover Search Backoff Max")]
    [Description("A bot in a shot reaction whose search found no cover waits longer each time (0.2, 0.4, 0.8 ... s) up to this many seconds, instead of searching every 0.1 s.")]
    [MinMax(0.1f, 10f, 10f)]
    public float ShotCoverEmptyBackoffMax = 2f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Alerts And Reports Mark The Player Known")]
    [Description("On = a bot that gets an alert (hit, close bullet, kill, squad alert, man-down) or a report about the player takes the player as a known enemy at the rough point (never the exact position): SAIN's combat layer then takes over from patrol and runs the cover, search and suppression logic. Off = the bot only stores the rough area and keeps walking its patrol.")]
    public bool ShotAlertMarksKnown = true;

    [DataMember]
    [Category("Shot Reaction/Duel")]
    [Name("Duel Retreat Enabled")]
    [Description("A bot that has been seeing the player for a long time, has had hits or close bullets of the player meanwhile and has not hit the player itself in that time gives the position up (not at close range): a full retreat out of the player's sight, then the normal hold and investigation. Off = it keeps fighting.")]
    public bool ShotDuelRetreatEnabled = true;

    [DataMember]
    [Category("Shot Reaction/Duel")]
    [Name("Duel: Seen For (s)")]
    [Description("Seconds the bot has to have seen the player in total (gaps under 15 s count as one sighting).")]
    [MinMax(10f, 600f, 1f)]
    public float ShotDuelSeenTime = 60f;

    [DataMember]
    [Category("Shot Reaction/Duel")]
    [Name("Duel: Shots On It Needed")]
    [Description("Hits by the player on this bot, or bullets of the player passing close to it, within the window below. The bot must not have hit the player itself in the same time.")]
    [MinMax(1f, 10f, 1f)]
    public float ShotDuelShots = 2f;

    [DataMember]
    [Category("Shot Reaction/Duel")]
    [Name("Duel: Hit Window (s)")]
    [Description("The hits and close bullets have to fall within this many seconds, and the bot must not have hit the player in that time.")]
    [MinMax(10f, 600f, 1f)]
    public float ShotDuelWindow = 60f;

    [DataMember]
    [Category("Shot Reaction/Duel")]
    [Name("Duel: Cooldown (s)")]
    [Description("A bot that has made a duel retreat does not make another for this many seconds.")]
    [MinMax(0f, 1200f, 1f)]
    public float ShotDuelCooldown = 120f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Area Narrowing Shared By Squad")]
    [Description("On = the count that narrows a bot's rough areas belongs to its squad: a piece of information about the player (a shot at one member, an alert, a report) narrows the ring for every member at once. Off = each bot counts for itself.")]
    public bool AreaShareSquad = true;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Area Narrowing Shared Radius")]
    [Description("When the ring of a Rogue or Goon narrows, the Rogues and Goons within this many meters of it (other squads too) get the same count at once. 0 = off.")]
    [MinMax(0f, 300f, 1f)]
    public float AreaShareRadius = 60f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Close Bullet Stack Step")]
    [Description("Every close bullet within the stack window adds this much to the chance (0.2 = 20% for the first bullet, 22% for the second with 0.02) that a bot in cover moves to another cover, up to the ceiling. A move resets it.")]
    [MinMax(0f, 0.2f, 100f)]
    public float ShotSelfPreserveBulletStep = 0.02f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Close Bullet Stack Ceiling")]
    [Description("The most the stacked close-bullet chance can reach, so suppression alone does not keep a bot moving.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSelfPreserveBulletCeiling = 0.5f;

    [DataMember]
    [Category("Shot Reaction/Self preservation")]
    [Name("Close Bullet Stack Reset (s)")]
    [Description("Seconds without a close bullet after which the stack starts again from the first bullet.")]
    [MinMax(1f, 120f, 1f)]
    public float ShotBulletStackReset = 20f;

    [DataMember]
    [Category("Shot Reaction/Rough area")]
    [Name("Sightings Shared As Rough Points In All Squads")]
    [Description("On = when a PMC, raider, scav or cultist sees the player, its squadmates get a rough point inside a ring (after the squad alert delay), narrowed like every rough area, instead of the exact position at once. Rogues and Goons always work this way (see Delayed Call-In). Bosses and followers keep the vanilla exact sharing. Off = ordinary squads share exactly.")]
    public bool ShotSightingRingsSquads = true;

    public override void Init(List<ISAINSettings> list)
    {
        list.Add(this);
    }
}
