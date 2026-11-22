using System.Collections.Generic;
using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories;

/// <summary>
/// Settings for behavior that only exists in this fork. Menu layout: Category = "Section/Subsection", drawn in the order of declaration.
/// Sections: Shot Reaction (several, by topic), Duel, Rogue Behaviour, then Range, Reload, Doors and Combat Cover. Every description ends with "Related:" (what else the setting touches).
/// Defaults are the owner's values (No Bullshit preset).
/// </summary>
[DataContract]
public class ForkSettings : SAINSettingsBase<ForkSettings>, ISAINSettings
{

    [DataMember]
    [Category("Shot Reaction: Basics/Switches")]
    [Name("Shot Reaction")]
    [Description("Master switch. A bot shot at by an enemy it cannot see rolls a reaction (take cover, fight back after a delay, suppress, or hunt the shooter) instead of instantly turning on you. The position it learns from the shot is vague, and the shooter is forgotten if the bot never sees it again. Off = original behavior. Related: Master switch: off disables the first roll, the Duel and most of Rogue Behaviour (radio call-ins, pressure retreat) for every bot. The brain filter below picks which bot types use it.")]
    public bool ShotReactionEnabled = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Switches")]
    [Name("Shot Reaction: Player Shooters Only")]
    [Description("Only react this way to shots from you. Shots from other bots keep the original behavior, which also keeps the cost down. Related: Also limits the duel, the pressure retreat, rough rings and alerts to shots and hits from you.")]
    public bool ShotReactionPlayerOnly = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: PMCs")]
    [Description("PMCs (USEC and BEAR) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainPmc = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Rogues")]
    [Description("Rogues (exUsec, Lighthouse) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainRogue = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Raiders")]
    [Description("Raiders (pmcBot: Labs, Reserve) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainRaider = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Scavs")]
    [Description("Regular scavs, scav groups and tagged-and-cursed scavs use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainScav = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Scav Snipers")]
    [Description("Scav snipers (marksman) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainScavSniper = false;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Cultists")]
    [Description("Cultists use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainCultist = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Bosses")]
    [Description("Bosses (not the Goons) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainBoss = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Boss Followers")]
    [Description("Boss guards (not the Goons) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainFollower = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Goons")]
    [Description("Knight, Big Pipe and Birdeye use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainGoon = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Event Bots")]
    [Description("Event bots (crazy scav event, gifter) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainEvent = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Brain filter")]
    [Name("Brain: Other Bots")]
    [Description("Any bot type not listed above (zombies, BTR, arena fighters, Labyrinth bots, ...) use the fork's shot reaction. Related: Applies to all Shot Reaction settings (roll, duel, pressure, alerts), not only this section. Rogue Behaviour also needs Rogues or Goons on. Range has its own brain filter.")]
    public bool ShotBrainOther = false;

    [DataMember]
    [Category("Shot Reaction: Basics/Logging")]
    [Name("Shot Reaction: Log Rolls")]
    [Description("Write every reaction roll (distance, reach, weights, outcome) to the BepInEx log. For tuning. Related: Logging only; the lines are what the duel, pressure retreat, alerts and rolls print.")]
    public bool ShotReactionLog = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Logging")]
    [Name("Log Bots Aiming At You")]
    [Description("While a bot shoots at you, write one line per bot (at most every 1.5 s) to the BepInEx log: distance, the aim height above your feet, the part it chose, whether only your head was shootable, the head scatter applied, its optic, fire mode and your pose. For finding out why a shot landed where it did. Related: Works with the other settings in Shot Reaction: Basics > Logging.")]
    public bool AimAtPlayerLog = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Logging")]
    [Name("Performance Log")]
    [Description("Every 10 seconds one log line with how much time the shot reaction code took (average ms per second, worst call, worst frame, per part) and how many vanilla report calls came in and were taken. Related: Works with the other settings in Shot Reaction: Basics > Logging.")]
    public bool ShotPerfLog = true;

    [DataMember]
    [Category("Shot Reaction: Basics/Logging")]
    [Name("Report Delivery Frame Budget (ms)")]
    [Description("Delivering a Rogue report to its group costs a few ms. All bots together deliver reports until this many ms are used in one frame; the rest wait for the next frame (the first report of a frame always goes through). 0 = no limit. Related: Works with Performance Log, which shows how many reports were pushed to a later frame.")]
    [MinMax(0f, 5f, 10f)]
    public float ReportFrameBudgetMs = 1f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Position Error Max")]
    [Description("The position error / uncertainty (meters) up to which it is taken as it is. Above it the value only creeps toward Position Error Soft Max (see there), so it takes extreme circumstances to get near that. Related: Knee of the cap on the ring radius and the uncertainty; concealment, audibility and environment widen the error before it. Set Soft Max at or below this for a hard ceiling.")]
    [MinMax(0f, 300f, 1f)]
    public float ShotPositionErrorMax = 50f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Position Error Min")]
    [Description("The smallest position error, in meters: a shot never gives the bot the shooter's exact position, even up close. Related: Floor of the ring radius (a shot never gives the exact position). Area Floor (Rough area rings) can keep it larger, and narrowing never goes below it.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotPositionErrorMin = 4f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Position Error Soft Max")]
    [Description("The value the position error and the uncertainty approach above Position Error Max, in meters. Between the two the error creeps up slowly (see Soft Length), so only extreme circumstances get near it. Related: Upper limit of the ring radius and the uncertainty; at or below Position Error Max it is a hard ceiling at Position Error Max.")]
    [MinMax(0f, 300f, 1f)]
    public float ShotPositionErrorSoftMax = 75f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Position Error Soft Length")]
    [Description("How slowly the error creeps from Position Error Max toward Soft Max, in meters of raw error: after this many meters above the knee it has covered about 63% of the way (100 and a knee of 50 means a raw 150 m gives about 65 m). Larger = slower. Related: Shape of the soft cap, nothing else.")]
    [MinMax(1f, 1000f, 1f)]
    public float ShotPositionErrorSoftLength = 100f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Error Per Meter Floor")]
    [Description("The base error is never less than the distance to the shooter times this (0.10 = 10 m at 100 m), so a bot with a long view distance does not place a far shooter exactly. Concealment, audibility and the environment multiply it. 0 = off. Related: Minimum base error before the factors; the sight-based error (Uncertainty At Sight) is used when it is larger.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotErrorPerMeter = 0.10f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Crouch Shooter Error Factor")]
    [Description("The position error is multiplied by this when the shooter fires crouched (a smaller silhouette, harder to place). 1 = no effect. Related: Multiplies the position error together with the other shooter factors (prone, bush, gear, hit).")]
    [MinMax(1f, 4f, 100f)]
    public float ShotCrouchErrorFactor = 1.15f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Gear Error Weight")]
    [Description("How much of the shooter's gear stealth value (backpack, headwear, face cover, rig; the same value the vision code uses) multiplies the position error. 1 = fully, 0 = not at all. Related: Stealthier gear gives a larger error; the value is the one from the gear stealth table of the preset.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGearErrorWeight = 1f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Prone Shooter Error Factor")]
    [Description("The position error (before the cap) is multiplied by this when the shooter lies prone: harder to place. 1 = no effect. Stacks with the suppressed factor. Related: Multiplies the position error together with the other shooter factors (suppressed, hit, bush, tracer); the result is still capped by Position Error Max.")]
    [MinMax(1f, 4f, 100f)]
    public float ShotProneErrorFactor = 1.3f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Position error")]
    [Name("Hit Position Error Factor")]
    [Description("The position error is multiplied by this when the bot was actually hit (a hit says more about where the shooter is). Related: Applies only to hits on the bot; capped by Position Error Max.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotHitErrorFactor = 0.85f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty")]
    [Name("Uncertainty High")]
    [Description("The uncertainty (meters, uncapped: distance x error per meter x hit x concealment x weather x light) at which the uncertainty effects below are at full strength. 0 = off. Related: The value at which every 'Uncertainty effects' setting reaches full strength. A higher value makes those effects weaker at the same distance.")]
    [MinMax(0f, 500f, 1f)]
    public float ShotUncertaintyHigh = 50f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty")]
    [Name("Uncertainty At Sight")]
    [Description("The uncertainty (meters) of a miss from a standing, loud shooter at exactly the bot's view distance (its current effective view distance for that shooter: weather, night, goggles, lights, movement, flare and gear are already in it). Closer it falls in proportion, down to 0 at the bot. Related: Base of the uncertainty that Uncertainty High, the Uncertainty effects and the ring size read. Capped by Position Error Max.")]
    [MinMax(1f, 60f, 10f)]
    public float ShotUncertaintyAtSight = 20f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty")]
    [Name("Uncertainty Beyond Sight")]
    [Description("Extra uncertainty (meters) for every further view distance the shooter is beyond the bot's sight: 15 = 35 m at twice the view distance, 50 m at three times. The total is capped by Position Error Max. Related: Added on top of Uncertainty At Sight for a shooter further than the bot's view distance; capped by Position Error Max.")]
    [MinMax(0f, 60f, 10f)]
    public float ShotUncertaintyBeyondSight = 15f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty")]
    [Name("Uncertainty Min Sight")]
    [Description("The view distance (meters) is never taken as less than this when the uncertainty is computed (a very dark night would otherwise blow it up). Related: Floor for the view distance used in the uncertainty: affects Uncertainty At Sight and Beyond Sight.")]
    [MinMax(5f, 200f, 1f)]
    public float ShotUncertaintyMinSight = 30f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty")]
    [Name("Bush Shooter Error Factor")]
    [Description("The uncertainty of a guessed shot position is multiplied by this when the shooter fires from inside a bush (foliage around the shooter): a common ambush, harder to place. 1 = no effect. Stacks with prone and suppressed. Related: Multiplies the uncertainty with the other shooter factors; capped by Position Error Max.")]
    [MinMax(1f, 4f, 100f)]
    public float ShotBushErrorFactor = 1.3f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Audibility")]
    [Name("Audible Start")]
    [Description("Below this share of the shot's audible range (distance divided by the range the weapon is heard at, with its suppressor and caliber) the shot is placed as well as before. Above it the position error grows. Related: Start of the audibility curve; the range comes from the weapon the shooter holds (hearing distance by caliber plus muzzle devices, times the suppressor modifier).")]
    [MinMax(0.1f, 2f, 100f)]
    public float ShotAudibleStart = 0.7f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Audibility")]
    [Name("Audible Peak Ratio")]
    [Description("The share of the audible range at which the audibility factor reaches Audible Peak Factor (1.3 = 30% beyond the range the shot is heard at). Related: Steepness of the audibility curve together with Audible Start.")]
    [MinMax(0.2f, 4f, 100f)]
    public float ShotAudiblePeakRatio = 1.3f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Audibility")]
    [Name("Audible Peak Factor")]
    [Description("The multiplier on the position error at the peak ratio (the curve rises smoothly from 1 at the start). A shot barely heard is placed badly. Related: Multiplies the position error with the other factors; capped by the soft cap.")]
    [MinMax(1f, 20f, 100f)]
    public float ShotAudiblePeakFactor = 4f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Audibility")]
    [Name("Audible Max Factor")]
    [Description("Beyond the peak ratio the factor keeps rising slowly, up to this value, which it reaches one full audible range further out. Related: Ceiling of the audibility factor.")]
    [MinMax(1f, 30f, 100f)]
    public float ShotAudibleMaxFactor = 6f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Tracers")]
    [Name("Tracer First Factor")]
    [Description("The position error is multiplied by this when a tracer round of the shooter first flew past or landed near the bot: a tracer shows where it came from, a little. Related: Start of the tracer ramp; it falls to Tracer Min Factor over the Ramp Shots.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotTracerFirstFactor = 0.95f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Tracers")]
    [Name("Tracer Min Factor")]
    [Description("The lowest multiplier the tracer ramp reaches during sustained tracer fire. Related: End of the tracer ramp; also sets how far the suppressor is cancelled (see Tracer Suppressor Cancel).")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotTracerMinFactor = 0.4f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Tracers")]
    [Name("Tracer Ramp Shots")]
    [Description("How many tracer rounds noticed (spaced at least 0.08 s apart, minus the slow decay) it takes to get from the first factor to the minimum. Related: Length of the tracer ramp; fewer rounds suit slow shooters.")]
    [MinMax(2f, 30f, 1f)]
    public float ShotTracerRampShots = 8f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Tracers")]
    [Name("Tracer Decay Time")]
    [Description("Seconds for the tracer ramp to lose one step while no new tracer is noticed: the ramp forgets slowly instead of resetting. 60 means one step per minute. Related: Memory of the tracer ramp; a bot only uses the ramp while the shooter is inside its rough area (see Tracer Ring Margin).")]
    [MinMax(1f, 600f, 1f)]
    public float ShotTracerDecayTime = 60f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Tracers")]
    [Name("Tracer Max Memory")]
    [Description("Seconds after the last noticed tracer round after which the whole ramp is forgotten, whatever steps are left. Related: Upper limit of the tracer memory together with Tracer Decay Time.")]
    [MinMax(5f, 1200f, 1f)]
    public float ShotTracerMaxMemory = 180f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Tracers")]
    [Name("Tracer Ring Margin")]
    [Description("A bot uses the tracer ramp only while the shooter is inside the rough area it holds: closer to where it last placed the shooter than the area's radius times this. 1 = exactly the radius; a bot without a fresh area uses the ramp. Moving out of the area switches the ramp off for that bot (the steps are kept and come back if the shooter returns). Related: Decides for each bot whether the tracer ramp applies; the area is the one from the position error model.")]
    [MinMax(0.5f, 4f, 100f)]
    public float ShotTracerRingMargin = 1.15f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Tracers")]
    [Name("Tracer Suppressor Cancel")]
    [Description("How much of a suppressor's effect (the shorter audible range) a sustained tracer fire cancels: the audible range moves this share of the way back to the unsuppressed range, scaled by how far the ramp has come. 0 = not at all. Related: Only matters with a suppressed weapon; reads the same audibility curve.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotTracerSuppressorCancel = 0.6f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Environment")]
    [Name("Inside To Outside Start")]
    [Description("A shooter inside a building (its environment id is above 0) and a bot outside: no extra error up to this distance in meters, then the error grows (see Inside To Outside Full and Factor). Related: Start of the ramp that makes it hard to tell which building a shot came from.")]
    [MinMax(0f, 500f, 1f)]
    public float ShotEnvInOutStart = 60f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Environment")]
    [Name("Inside To Outside Full")]
    [Description("The distance in meters at which the inside-to-outside multiplier is at full strength. Related: End of the inside-to-outside ramp.")]
    [MinMax(1f, 1000f, 1f)]
    public float ShotEnvInOutFull = 200f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Environment")]
    [Name("Inside To Outside Factor")]
    [Description("The position error multiplier for a shooter inside and a bot outside at the full distance (smooth rise from 1 at the start). 1 = off. Related: Multiplies the position error with the other factors; building to building and outside to outside are unchanged.")]
    [MinMax(1f, 10f, 100f)]
    public float ShotEnvInOutFactor = 2.5f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Environment")]
    [Name("Outside To Inside Start")]
    [Description("A shooter outside and a bot inside a building: no extra error up to this distance in meters. Related: Start of the outside-to-inside ramp.")]
    [MinMax(0f, 500f, 1f)]
    public float ShotEnvOutInStart = 100f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Environment")]
    [Name("Outside To Inside Full")]
    [Description("The distance in meters at which the outside-to-inside multiplier is at full strength. Related: End of the outside-to-inside ramp.")]
    [MinMax(1f, 1000f, 1f)]
    public float ShotEnvOutInFull = 250f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Environment")]
    [Name("Outside To Inside Factor")]
    [Description("The position error multiplier for a shooter outside and a bot inside at the full distance. 1 = off. Related: Multiplies the position error with the other factors.")]
    [MinMax(1f, 10f, 100f)]
    public float ShotEnvOutInFactor = 1.5f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty effects")]
    [Name("Uncertainty Fight Floor")]
    [Description("At full uncertainty the Fight share of the first reaction roll is multiplied by this (1 = no effect): a bot that cannot tell where the shot came from fights less. Related: Scales the Fight share of the first roll (The First Roll) down as the uncertainty rises toward Uncertainty High.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotUncertaintyFightMin = 0.3f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty effects")]
    [Name("Uncertainty Investigate Floor")]
    [Description("At full uncertainty the Investigate weight of the follow-up pick after hiding is multiplied by this (1 = no effect): a bot that cannot tell where the shot came from goes looking less. Related: Scales the Investigate weight after hiding (Follow-up pick) and the advance-without-cover chance (Investigate) down with the uncertainty.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotUncertaintyInvestigateMin = 0.4f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty effects")]
    [Name("Fight Fire Max Uncertainty")]
    [Description("Fight On and the fight search fire at the rough shot area only while its uncertainty (meters) is at most this. Above it the bot holds fire. 0 = no limit. Related: Limits Fight On fire and the Fight search (Fight outcome): beyond this uncertainty the bot only watches.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotFightFireMaxUncertainty = 30f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty effects")]
    [Name("Grenade Max Uncertainty")]
    [Description("A bot throws a grenade at the rough shot area only while its uncertainty (meters) is at most this (a grenade's splash is only a few meters wide). 0 = no limit. Related: Limits the grenades in Squad and Grenades.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotNadeMaxUncertainty = 20f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty effects")]
    [Name("Uncertainty Fire Time Floor")]
    [Description("At full uncertainty (Uncertainty High) the burst length of Fight On is multiplied by this: a bot that cannot tell where the shooter is fires shorter bursts. Related: Shortens the Fight On burst length (Fight outcome: Fire Time) at full uncertainty.")]
    [MinMax(0.05f, 1f, 100f)]
    public float ShotUncertaintyFireTimeMin = 0.3f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Uncertainty effects")]
    [Name("Uncertainty Pause Factor")]
    [Description("At full uncertainty (Uncertainty High) the pause between Fight On bursts is multiplied by this. Related: Lengthens the Fight On pause (Fight outcome: Fire Pause) at full uncertainty.")]
    [MinMax(1f, 10f, 10f)]
    public float ShotUncertaintyPauseMax = 3f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Rough Area Inner Fraction")]
    [Description("The rough area of a shot, hit or alert is a random point in a ring around the shooter: not closer than this share of the area's radius. 0.4 = a 50 m area never lands within 20 m of the shooter, so a lucky draw cannot put a searching bot on top of the player. 0 = anywhere in the disk (old behaviour). Related: Inner edge of every ring: shots, hits, alerts, Rogue reports and squad sightings. Narrowing (Area Shrink) shrinks the outer edge and Area Floor limits it.")]
    [MinMax(0f, 0.9f, 100f)]
    public float ShotAreaInnerFraction = 0.4f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Shrink Per Piece")]
    [Description("Every piece of information about the player's position that reaches a bot (a shot, hit, alert or report; at most one per Cooldown) makes its rough areas this much narrower (0.92 = 8% each, the radius and the uncertainty). Counts are forgotten after Area Memory seconds without any. 1 = areas never narrow. Related: Works with Area Shrink Cooldown (when a piece counts), Area Floor (the limit) and Area Memory (reset). Shared with the squad (Shared By Squad) or with nearby Rogues and Goons (Shared Radius).")]
    [MinMax(0.5f, 1f, 100f)]
    public float AreaShrink = 0.92f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Shrink Cooldown")]
    [Description("Seconds between two counted pieces of information: a full-auto burst, several alerts or reports in a few seconds count once. Related: Decides which shots, hits, alerts and reports count as a new piece for Area Shrink, in all brains and in Rogue reports.")]
    [MinMax(0f, 30f, 10f)]
    public float AreaShrinkCooldown = 5f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Floor Fraction")]
    [Description("Narrowing never takes a rough area (its radius and uncertainty) below this share of the error it started with (0.5 = at most halved), so a far or hard to place shooter stays vague however many pieces arrive. Related: Limit of Area Shrink Per Piece; the starting error is the position error model (audibility, environment, concealment). Tracer fire lowers it (see Area Floor Fraction With Tracers).")]
    [MinMax(0f, 1f, 100f)]
    public float AreaFloorFraction = 0.5f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Floor Fraction With Tracers")]
    [Description("The floor share when tracer fire has fully ramped (it moves from Area Floor Fraction to this as the ramp builds): a shooter firing tracers can be narrowed further. Related: Reads the tracer ramp (Tracers section).")]
    [MinMax(0f, 1f, 100f)]
    public float AreaFloorFractionTracer = 0.25f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Far Piece Weight")]
    [Description("How much a piece of information counts toward the narrowing when the shot is barely audible (at the audibility peak ratio or beyond): 1 = the same as any, 0 = not at all. It fades in from Audible Start. Hits always count in full. Related: Reads the audibility curve (Audibility section); weights the pieces of Area Shrink Per Piece.")]
    [MinMax(0f, 1f, 100f)]
    public float AreaFarPieceWeight = 0.25f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Sharing")]
    [Name("Share Base Error")]
    [Description("Information a bot passes on (a sighting report, a squad or Rogue alert) is exact at its source, but never exact at the listener: this much error (meters) is added for everyone, even the squadmate next to the source. It is the small non-determinism of a radio call. Related: Added to the source's own error, then Share Free Range, Share Near Per Meter and Share Far Per Meter add the distance blur; the soft cap applies last.")]
    [MinMax(0f, 20f, 10f)]
    public float ShareBaseError = 2f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Sharing")]
    [Name("Share Free Range")]
    [Description("Listeners within this distance (meters) of the source only get the base error. Beyond it the blur grows: slowly up to Share Near End, faster after it. Related: Like pointing at a tree: a mate next to you knows which, one on the other side of the map does not.")]
    [MinMax(0f, 200f, 1f)]
    public float ShareFreeRange = 20f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Sharing")]
    [Name("Share Near Per Meter")]
    [Description("Extra error (meters per meter) between Share Free Range and Share Near End.")]
    [MinMax(0f, 1f, 100f)]
    public float ShareNearPerMeter = 0.05f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Sharing")]
    [Name("Share Near End")]
    [Description("Distance (meters) from the source where the blur starts to grow faster (Share Far Per Meter).")]
    [MinMax(0f, 500f, 1f)]
    public float ShareNearEnd = 100f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Sharing")]
    [Name("Share Far Per Meter")]
    [Description("Extra error (meters per meter) beyond Share Near End, before the soft cap. Related: The soft cap (Position Error Max / Soft Max) limits the total.")]
    [MinMax(0f, 2f, 100f)]
    public float ShareFarPerMeter = 0.2f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Tracer Piece Bonus")]
    [Description("Extra weight of a piece at full tracer ramp (1 = counts double). Related: Reads the tracer ramp; weights the pieces of Area Shrink Per Piece.")]
    [MinMax(0f, 4f, 100f)]
    public float AreaTracerPieceBonus = 1f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Tracer Cooldown Cut")]
    [Description("How much of Area Shrink Cooldown tracer fire cuts at full ramp (0.6 = 60% shorter), so a burst of tracers narrows the area faster. Related: Shortens the time between counted pieces while tracers are noticed.")]
    [MinMax(0f, 0.9f, 100f)]
    public float AreaTracerCooldownCut = 0.6f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Concealment Weight")]
    [Description("How strongly the shooter's concealment (prone, crouched, bush, gear) slows the narrowing: a piece's weight is divided by the concealment factor scaled by this. 1 = fully, 0 = not at all. Related: Reads the same concealment factor as the position error; weights the pieces of Area Shrink Per Piece.")]
    [MinMax(0f, 1f, 100f)]
    public float AreaConcealmentWeight = 1f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Memory")]
    [Description("Seconds without any information about the player after which a bot's areas start wide again. Related: After this long without any information the narrowing resets for the bot or the squad (see Shared By Squad).")]
    [MinMax(5f, 600f, 1f)]
    public float AreaMemory = 60f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Narrowing Shared By Squad")]
    [Description("On = the count that narrows a bot's rough areas belongs to its squad: a piece of information about the player (a shot at one member, an alert, a report) narrows the ring for every member at once. Off = each bot counts for itself. Related: Ordinary squads (PMCs, raiders, scavs, cultists). Rogues and Goons use Shared Radius instead.")]
    public bool AreaShareSquad = true;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Area Narrowing Shared Radius")]
    [Description("When the ring of a Rogue or Goon narrows, the Rogues and Goons within this many meters of it (other squads too) get the same count at once. 0 = off. Related: Rogues and Goons only. Shares the narrowing, and a hit or close bullet at one keeps the pressure clocks of the others running (Pressure retreat, Rogue Behaviour). Ordinary squads use Shared By Squad.")]
    [MinMax(0f, 300f, 1f)]
    public float AreaShareRadius = 60f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Alerts And Reports Mark The Player Known")]
    [Description("On = a bot that gets an alert (hit, close bullet, kill, squad alert, man-down) or a report about the player takes the player as a known enemy at the rough point (never the exact position): SAIN's combat layer then takes over from patrol and runs the cover, search and suppression logic. Off = the bot only stores the rough area and keeps walking its patrol. Related: Decides whether a bot that gets a Rogue alert, squad alert, man-down or report starts SAIN's combat for you (ring, cover, investigate). Off = the alert only starts the shot reaction.")]
    public bool ShotAlertMarksKnown = true;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Rough area rings")]
    [Name("Sightings Shared As Rough Points In All Squads")]
    [Description("On = when a PMC, raider, scav or cultist sees the player, its squadmates get a rough point inside a ring (after the squad alert delay), narrowed like every rough area, instead of the exact position at once. Rogues and Goons always work this way (see Delayed Call-In). Bosses and followers keep the vanilla exact sharing. Off = ordinary squads share exactly. Related: Ordinary squads. Uses the Squad Alert Delay (Squad and Grenades) and the ring settings above. Rogue reports have their own settings in Rogue Behaviour.")]
    public bool ShotSightingRingsSquads = true;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Spotting under fire")]
    [Name("Under Fire Spot Coefficient")]
    [Description("Multiplier on the time a bot needs to spot a shooter that is firing at it, when Shot Reaction is on for that bot. Original SAIN value: 0.5 (spots twice as fast). 1 = no bonus for being shot at. Related: Applies only while the bot's shot reaction is on; also changes how fast a bot spots you in a duel.")]
    [MinMax(0.1f, 2f, 100f)]
    public float ShotUnderFireSpotCoef = 1f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Spotting under fire")]
    [Name("Close Spot Coefficient")]
    [Description("Multiplier on the time a bot needs to spot the shooter while its shot reaction is active and the shooter is within the Close Interrupt Distance. Low = the bot notices a shooter at arm's length almost at once. 1 = no bonus. Related: Applies only while the shot reaction is active and you are within the Close Range (Close Range section).")]
    [MinMax(0.01f, 1f, 100f)]
    public float ShotCloseSpotCoef = 0.15f;

    [DataMember]
    [Category("Shot Reaction: Knowing Where The Shooter Is/Spotting under fire")]
    [Name("Under Fire Vision Multiplier")]
    [Description("Multiplier on the distance a bot sees a shooter that is firing at it, when Shot Reaction is on for that bot. Original SAIN value: 1.5. 1 = no bonus. Related: Applies only while the shot reaction is on; stacks with the two spot coefficients above.")]
    [MinMax(0.5f, 3f, 100f)]
    public float ShotUnderFireVisionMulti = 1f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Short Zone End")]
    [Description("Shots from closer than this fraction of the bot's effective reach use the Short weights. Related: Zones are shares of the bot's weapon reach (Range > Sights changes the reach) after Distance Scale; they pick which weights apply.")]
    [MinMax(0.05f, 1f, 100f)]
    public float ShotZoneShort = 0.25f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Mid Zone End")]
    [Description("Shots from closer than this fraction of the bot's effective reach (and farther than the short zone) use the Mid weights. Farther = Long weights. Related: See Short Zone End. Beyond this and the reach the Long weights apply.")]
    [MinMax(0.1f, 3f, 100f)]
    public float ShotZoneMid = 0.75f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Distance Scale")]
    [Description("Multiplies the distance (as a share of the bot's reach) before the zone weights are read. Below 1 every distance counts as shorter, so bots fight more; above 1 they hide more. Does not change point blank, close range, or the fade of Fight beyond the real reach. Related: Scales the distance before the zones are read: affects every weight, Far Range and the weapon bonuses.")]
    [MinMax(0.3f, 2f, 100f)]
    public float ShotDistanceScale = 0.8f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Short: Cover Weight")]
    [Description("Relative weight of taking cover when the shooter is in the short zone. The four weights of a zone are compared, they do not need to add up to 100. Related: Compared with the other weights of the zone, then moved by personality (Pulls), limited by Cover Min Share and Fight Max Share and reduced by uncertainty.")]
    [MinMax(0f, 100f, 1f)]
    public float ShortCover = 15f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Short: Fight Weight")]
    [Description("Relative weight of fighting back (after a reaction delay) in the short zone. Related: Compared with the other weights of the zone, then moved by personality, limited by Fight Max Share and reduced by the uncertainty (Uncertainty Fight Floor).")]
    [MinMax(0f, 100f, 1f)]
    public float ShortFight = 75f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Mid: Cover Weight")]
    [Description("Relative weight of taking cover in the mid zone. Related: Compared with the other weights of the zone, then moved by personality, limited by Cover Min Share and reduced by uncertainty.")]
    [MinMax(0f, 100f, 1f)]
    public float MidCover = 35f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Mid: Fight Weight")]
    [Description("Relative weight of fighting back in the mid zone. Related: Compared with the other weights of the zone, then moved by personality, limited by Fight Max Share and reduced by the uncertainty.")]
    [MinMax(0f, 100f, 1f)]
    public float MidFight = 50f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Long: Cover Weight")]
    [Description("Relative weight of taking cover in the long zone. Related: Compared with the other weights of the zone, then moved by personality, limited by Cover Min Share and reduced by uncertainty.")]
    [MinMax(0f, 100f, 1f)]
    public float LongCover = 95f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Long: Fight Weight")]
    [Description("Relative weight of fighting back in the long zone. Only bots whose weapon reaches the shooter can pick it. Related: Compared with the other weights of the zone, then moved by personality, limited by Fight Max Share and reduced by the uncertainty. Far Range can turn a Fight into Cover.")]
    [MinMax(0f, 100f, 1f)]
    public float LongFight = 2f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Strong Weapon Range")]
    [Description("A shooter closer than this share of the bot's effective reach counts as close for a strong weapon: the Cover share is limited to the next setting there. Related: Works with Strong Weapon Cover Limit and the weapon bonuses (Weapon and reach).")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotStrongWeaponRatio = 0.5f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Zones and weights")]
    [Name("Strong Weapon Cover Limit")]
    [Description("Largest Cover share when the shooter is inside the Strong Weapon Range. The rest goes to fighting back. Related: Applies inside Strong Weapon Range; Cover Min Share still keeps a floor.")]
    [MinMax(0.2f, 1f, 100f)]
    public float ShotStrongWeaponCoverMax = 0.6f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Personality and shooter pulls")]
    [Name("Cover Min Share")]
    [Description("The share (0-1) of rolls that always goes to Cover, whatever the personality and weapon pull. Keeps bots from always fighting back. Related: Applied after personality, weapon, running and uncertainty adjustments; a stronger Cover Min Share makes Fight Max Share matter less.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotCoverMinShare = 0.35f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Personality and shooter pulls")]
    [Name("Fight Max Share")]
    [Description("The most (0-1) the Fight outcome can get of a roll, whatever the personality and weapon pull. Related: Applied after personality, weapon and uncertainty adjustments; Cover Min Share is the opposite limit.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFightMaxShare = 0.65f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Personality and shooter pulls")]
    [Name("Aggressive Personality Pull")]
    [Description("Wreckless, Giga Chad and Chad multiply their Fight, Suppress and Hunt weights by this and divide their Cover weight by it. Related: Also scales the post-hide follow-up weights, the advance-without-cover chance, the hold and ambush times, and the duel resolve uses the same aggressive personalities (+0.2).")]
    [MinMax(1f, 4f, 10f)]
    public float ShotAggressivePull = 1.5f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Personality and shooter pulls")]
    [Name("Timid Personality Pull")]
    [Description("Rat, Timmy and Coward multiply their Cover weight by this and divide their Fight, Suppress and Hunt weights by it. Related: Also scales the follow-up weights, hold times and the post-retreat safety level; the duel resolve uses the same timid personalities (-0.2).")]
    [MinMax(1f, 4f, 10f)]
    public float ShotTimidPull = 1.5f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Personality and shooter pulls")]
    [Name("Suppressed Shooter Factor")]
    [Description("Fight, Suppress and Hunt weights are multiplied by this when the shooter uses a suppressor (harder to place). Related: Multiplies the same weights as the prone factor; the position error has its own factors (Position error).")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotSuppressedShooterFactor = 0.6f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Personality and shooter pulls")]
    [Name("Prone Shooter Factor")]
    [Description("Fight, Suppress and Hunt weights are multiplied by this when the shooter is prone (harder to place). Related: Multiplies the same weights as the suppressed factor; the position error has its own factor (Position error).")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotProneShooterFactor = 0.7f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Weapon and reach")]
    [Name("Reference Reach")]
    [Description("Up close, every weapon is judged against this reach (meters) instead of its own, so a long-range weapon does not count as 'very close' at 18 m. The bot's own reach takes over linearly up to the full effect distance. Related: Up close this replaces the bot's own reach for the roll; Reach Full Effect Distance decides how fast the bot's own reach takes over.")]
    [MinMax(20f, 300f, 1f)]
    public float ShotReferenceReach = 60f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Weapon and reach")]
    [Name("Reach Full Effect Distance")]
    [Description("From this distance (meters) on, the bot's own weapon reach decides the roll completely. Closer, the reference reach is blended in. Related: With Reference Reach decides how weapon reach enters the roll; also the fade of the weapon-type bonuses (Weapon Type Effect Distance).")]
    [MinMax(10f, 300f, 1f)]
    public float ShotReachFullEffectDistance = 60f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Weapon and reach")]
    [Name("Weapon Type Effect Distance")]
    [Description("The weapon type factors below apply fully up to this distance (meters) and fade out to nothing at twice this distance. Related: The weapon-type factors below fade out beyond this distance.")]
    [MinMax(5f, 150f, 1f)]
    public float ShotWeaponEffectDistance = 30f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Weapon and reach")]
    [Name("SMG / Shotgun Fight Bonus")]
    [Description("Fight weight multiplier for submachine guns and shotguns up close. 1 = no effect. Related: Works with the other settings in Shot Reaction: The First Roll > Weapon and reach.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponShortGunFightBonus = 1.3f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Weapon and reach")]
    [Name("Marksman / Sniper Cover Bonus")]
    [Description("Cover weight multiplier for marksman and sniper rifles up close (they are poor at close range). 1 = no effect. Related: Works with the other settings in Shot Reaction: The First Roll > Weapon and reach.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponLongGunCoverBonus = 1.4f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Weapon and reach")]
    [Name("Full Auto Fight Bonus")]
    [Description("Fight weight multiplier for a weapon that can fire full auto, up close. 1 = no effect. Related: Works with the other settings in Shot Reaction: The First Roll > Weapon and reach.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponFullAutoFightBonus = 1.15f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Weapon and reach")]
    [Name("Semi Auto Only Cover Bonus")]
    [Description("Cover weight multiplier for a weapon that cannot fire full auto, up close. 1 = no effect. Related: Works with the other settings in Shot Reaction: The First Roll > Weapon and reach.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotWeaponSemiOnlyCoverBonus = 1.15f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Bot state")]
    [Name("Running Penalty Fade Distance")]
    [Description("A running bot leans toward cover (Cover x1.3, Fight x0.5). That effect is full at this distance (meters) and fades to nothing at the Point Blank Distance, so a running bot hit from close range does not just flee. Related: Running bots lean to cover; the fade ends at this distance. Facing and Back Turned angles below add to the same roll.")]
    [MinMax(10f, 100f, 1f)]
    public float ShotRunningFadeDistance = 30f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Bot state")]
    [Name("Facing Angle")]
    [Description("A bot whose look direction is within this many degrees of the shot direction counts as facing it. Related: Works with the other settings in Shot Reaction: The First Roll > Bot state.")]
    [MinMax(5f, 90f, 1f)]
    public float ShotFacingAngle = 30f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Bot state")]
    [Name("Facing Fight Bonus")]
    [Description("Fight weight multiplier for a bot that is facing the shot. 1 = no effect. Related: Works with the other settings in Shot Reaction: The First Roll > Bot state.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotFacingFightBonus = 1.3f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Bot state")]
    [Name("Back Turned Angle")]
    [Description("A bot whose look direction is at least this many degrees away from the shot direction counts as having its back turned. Related: Works with the other settings in Shot Reaction: The First Roll > Bot state.")]
    [MinMax(90f, 180f, 1f)]
    public float ShotBackTurnedAngle = 120f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Bot state")]
    [Name("Back Turned Cover Bonus")]
    [Description("Cover weight multiplier for a bot with its back turned to the shot. 1 = no effect. Related: Works with the other settings in Shot Reaction: The First Roll > Bot state.")]
    [MinMax(1f, 3f, 100f)]
    public float ShotBackTurnedCoverBonus = 1.3f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Far range")]
    [Name("Far Range Fight Goes To Cover First")]
    [Description("A Fight roll beyond the Far Range Start Distance may turn into Cover followed by a (boosted) Fight On: the bot sprints to cover and fires at the vague area from there, instead of turning around and blind firing at once. The chance ramps with distance (see the other Far Range settings). Off = Fight rolls always fight at once. Related: Uses absolute meters (Far Range settings), not the bot's reach: a scoped bot gets the same convert as a pistol bot. Works with the Fight weights and Fight On Boost.")]
    public bool ShotFarFightViaCover = true;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Far range")]
    [Name("Far Range Start Distance")]
    [Description("Meters. From this distance on, a Fight roll can turn into Cover then Fight On, with a chance that starts at 0 here. Lower it to start earlier. 0 = off. Related: Absolute meters (not scaled by reach). From here a Fight roll can turn into Cover then Fight On.")]
    [MinMax(0f, 400f, 1f)]
    public float ShotFarStartDistance = 80f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Far range")]
    [Name("Far Range Full Distance")]
    [Description("Meters. From this distance on the chance is at its maximum (Far Range Max Chance). Move it closer to the start distance to make the effect ramp up faster. Related: Absolute meters (not scaled by reach). The convert chance reaches Far Range Max Chance here.")]
    [MinMax(20f, 500f, 1f)]
    public float ShotFarFullDistance = 120f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Far range")]
    [Name("Far Range Max Chance")]
    [Description("The chance that a Fight roll turns into Cover then Fight On at the full distance and beyond. 85% leaves a rare true instant Fight. Related: Works with the other settings in Shot Reaction: The First Roll > Far range.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFarMaxConvert = 0.85f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Far range")]
    [Name("Far Range Curve")]
    [Description("Shape of the ramp between the start and the full distance. 1 = straight line, 2 = rises fast at first then flattens (chance at the halfway point is 75% of the maximum), 3 = faster still. Below 1 it starts slowly. Related: Works with the other settings in Shot Reaction: The First Roll > Far range.")]
    [MinMax(0.3f, 5f, 10f)]
    public float ShotFarCurve = 2f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Far range")]
    [Name("Far Range Fight On Boost")]
    [Description("A bot whose Fight roll turned into Cover picks its follow-up like any hiding bot (personality, injury, reach limit, squad), but with the Fight On weight multiplied by this. 1 = no boost. Related: Multiplies the Fight On weight of the follow-up (After Hiding > Follow-up pick) of a converted Fight.")]
    [MinMax(1f, 20f, 10f)]
    public float ShotFarFightOnBoost = 4f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Fight outcome")]
    [Name("Fight Duration")]
    [Description("A bot that chose to fight back uses normal SAIN combat behavior at once for this many seconds, then the shooter is forgotten if the bot has not seen it. Related: Time of the normal combat after a Fight roll; the Fight search settings below act inside it.")]
    [MinMax(2f, 60f, 1f)]
    public float ShotFightDuration = 10f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Fight outcome")]
    [Name("Fight: Search And Fire")]
    [Description("A bot that rolled Fight against a shooter it cannot see turns to the vague shot area, fires at it and works toward it, instead of leaving it to SAIN's normal combat (which walks around until it sees the shooter). Off = a Fight is plain SAIN combat at once. Related: Works with the other settings in Shot Reaction: The First Roll > Fight outcome.")]
    public bool ShotFightSearchEnabled = true;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Fight outcome")]
    [Name("Fight: Search Time")]
    [Description("Seconds a Fight bot searches the vague area this way (before normal SAIN combat takes over). Seeing the shooter ends it at once. Related: Works with the other settings in Shot Reaction: The First Roll > Fight outcome.")]
    [MinMax(1f, 15f, 0.5f)]
    public float ShotFightSearchTime = 4f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Fight outcome")]
    [Name("Fight: Move While Searching")]
    [Description("The searching Fight bot uses SAIN's dogfight movement (strafes, backs up when it must, closes in on the vague area at a run-walk) while it fires, instead of standing still. Off = it stands. Related: Works with the other settings in Shot Reaction: The First Roll > Fight outcome.")]
    public bool ShotFightSearchMove = true;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Fight outcome")]
    [Name("Fight: Fire Time")]
    [Description("Seconds of that stop during which the bot fires at the vague area (the rest it just watches it). Not fired when the area is farther than the weapon reach. Related: Also the burst length of Fight On, shortened by the uncertainty (Uncertainty Fire Time Floor).")]
    [MinMax(0f, 15f, 0.5f)]
    public float ShotFightFireTime = 2f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Fight outcome")]
    [Name("Fight On: Fire Pause")]
    [Description("A bot that fights on from cover (suppression) fires at the vague area for Fight: Fire Time, then pauses this many seconds, and repeats, for as long as the state lasts and it has not seen the shooter. 0 = SAIN's own suppression only. Related: Pause between Fight On bursts, lengthened by the uncertainty (Uncertainty Pause Factor).")]
    [MinMax(0f, 15f, 0.5f)]
    public float ShotFightOnFirePause = 4f;

    [DataMember]
    [Category("Shot Reaction: The First Roll/Timing")]
    [Name("Reroll Interval")]
    [Description("Minimum seconds between two reaction rolls for one bot. Shots in between only keep the current reaction going. Related: Also limits how often an alert or a squad sighting can start a reaction in the bot.")]
    [MinMax(0.5f, 30f, 10f)]
    public float ShotRerollInterval = 4f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Point blank and close range")]
    [Name("Point Blank Distance")]
    [Description("Hit or shot at from this close (meters) the bot fights back with the Point Blank Fight Share chance, with no concealment effect. Hard stop, no fade. 0 = off. Related: Inside this distance the Point Blank Fight Share replaces the zone weights.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotPointBlankDistance = 10f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Point blank and close range")]
    [Name("Point Blank Fight Share")]
    [Description("Chance of fighting back inside the Point Blank Distance. 95% leaves a little randomness. The rest is cover. Related: Works with the other settings in Shot Reaction: Close Range > Point blank and close range.")]
    [MinMax(0.3f, 1f, 100f)]
    public float ShotPointBlankFightShare = 0.95f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Point blank and close range")]
    [Name("Close Interrupt Distance")]
    [Description("A shooter this close (meters), shooting or not, always makes the bot fight: it interrupts a hide, hold, investigation or retreat already in effect. 0 = off. Related: Interrupts a hide, hold, investigation, retreat and the duel push when you walk up this close.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotCloseInterruptDistance = 5f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Point blank and close range")]
    [Name("Other Enemy Interrupt Distance")]
    [Description("Any enemy other than the shooter (a scav, another PMC, a Rogue ...) that the bot can see within this distance (meters), or that hits it, ends the reaction in effect (hide, hold, investigation, retreat): normal SAIN combat takes over and deals with that enemy. 0 = off. Related: Any other visible enemy this close interrupts the same states.")]
    [MinMax(0f, 50f, 1f)]
    public float ShotOtherEnemyInterruptDistance = 15f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Point blank and close range")]
    [Name("Close Range (share of reach)")]
    [Description("Close range is this share of the bot weapon reach, clamped by the min and max below. Close range: no cover hops when shot at in cover, and a hiding bot may fire back. Related: Defines 'close range', used by Cornered, the No cover bonuses, the Close Spot Coefficient, the duel (no duel at close range, the clock resets) and the repeat retreats (never against a close shooter).")]
    [MinMax(0.05f, 1f, 100f)]
    public float ShotCloseRangeReachFraction = 0.25f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Point blank and close range")]
    [Name("Close Range Minimum")]
    [Description("Close range is never smaller than this many meters. Related: Lower limit of close range; see Close Range (share of reach).")]
    [MinMax(0f, 50f, 1f)]
    public float ShotCloseRangeMin = 10f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Point blank and close range")]
    [Name("Close Range Maximum")]
    [Description("Close range is never larger than this many meters. Related: Upper limit of close range; see Close Range (share of reach).")]
    [MinMax(5f, 100f, 1f)]
    public float ShotCloseRangeMax = 25f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Cornered")]
    [Name("Cornered Hits")]
    [Description("A bot that hides or retreats and takes this many hits within the Cornered Window (from the Cornered Distance or closer) stops and fights. 0 = off. Related: Works with Cornered Window and Cornered Distance; a cornered bot fights instead of hiding or retreating.")]
    [MinMax(0f, 10f, 1f)]
    public float ShotCorneredHits = 3f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Cornered")]
    [Name("Cornered Window")]
    [Description("Seconds in which the Cornered Hits have to land. Related: Works with the other settings in Shot Reaction: Close Range > Cornered.")]
    [MinMax(1f, 30f, 1f)]
    public float ShotCorneredWindow = 5f;

    [DataMember]
    [Category("Shot Reaction: Close Range/Cornered")]
    [Name("Cornered Distance")]
    [Description("Hits from farther than this (meters) do not count as cornering: the bot keeps running for cover. Related: Works with the other settings in Shot Reaction: Close Range > Cornered.")]
    [MinMax(5f, 150f, 1f)]
    public float ShotCorneredDistance = 30f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("No Cover Grace Time")]
    [Description("Seconds the bot waits for the cover finder before deciding fight or flight when it has no cover. Related: Starts the fight-or-flight score (No cover section); the bonuses add up with the base score.")]
    [MinMax(0f, 3f, 10f)]
    public float ShotNoCoverGrace = 0.8f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Base Fight Score")]
    [Description("Starting score of the fight-or-flight decision. Positive = fights more. The fight chance is 1 / (1 + e^-score). Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(-5f, 5f, 10f)]
    public float ShotNoCoverBias = -0.5f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Close Range Bonus")]
    [Description("Added to the fight score when the shooter is within close range. Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverCloseBonus = 2f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Inside Half Reach Bonus")]
    [Description("Added when the shooter is within half of the weapon reach. Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverNearBonus = 1f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Inside Reach Bonus")]
    [Description("Added when the shooter is within the weapon reach (beyond half). Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverReachBonus = 0.3f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Just Out Of Reach Penalty")]
    [Description("Subtracted when the shooter is just beyond the reach (up to 1.25x). Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverJustOutPenalty = 1f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Far Out Of Reach Penalty")]
    [Description("Subtracted when the shooter is far beyond the reach. Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotNoCoverFarPenalty = 2.5f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Personality Weight")]
    [Description("Added for aggressive personalities, subtracted for timid ones. Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(0f, 5f, 10f)]
    public float ShotNoCoverPersonality = 1f;

    [DataMember]
    [Category("Shot Reaction: Close Range/No cover: fight or flight")]
    [Name("Hurt Penalty")]
    [Description("Subtracted when the bot is badly injured. Related: Works with the other settings in Shot Reaction: Close Range > No cover: fight or flight.")]
    [MinMax(0f, 5f, 10f)]
    public float ShotNoCoverHurtPenalty = 0.5f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover choice")]
    [Name("Thin Cover Penalty")]
    [Description("Cover whose collider name says thin, plastic, glass, cardboard or cloth (a rifle round goes through it) counts as this many meters farther away during a shot reaction, so a solid object a bit further off wins. 0 = thin cover is as good as a wall. Related: Part of the cover choice with Low Cover Penalty and Bush Path Penalty: all add meters to a cover's path length.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotCoverThinPenalty = 25f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover choice")]
    [Name("Cover: Low Height")]
    [Description("During a shot reaction a cover lower than this (meters) leaves the head exposed: it counts as Low Penalty meters farther away. 0 = off. Related: Works with the other settings in Shot Reaction: Cover > Cover choice.")]
    [MinMax(0f, 2f, 100f)]
    public float ShotCoverLowHeight = 1f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover choice")]
    [Name("Cover: Low Penalty")]
    [Description("Meters added to the path length of a low cover (see Low Height) when a bot in a shot reaction picks its cover. Related: Applies to covers lower than Low Cover Height; adds to the same path length as Thin Cover and Bush penalties.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotCoverLowPenalty = 15f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover choice")]
    [Name("Bush Path Penalty")]
    [Description("A bush counts as this many meters farther away when a bot picks cover, so solid cover wins unless a bush is that much closer. 0 = bushes are as good as walls. Related: Fades with the Bush Fade Start/End distances; adds to the same path length as the thin and low cover penalties.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotFoliagePenalty = 30f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover choice")]
    [Name("Bush Fade Start Distance")]
    [Description("A bush closer than this to the shot area keeps the full penalty. Beyond it, and out of the shot area line of sight, the penalty fades. Related: Works with the other settings in Shot Reaction: Cover > Cover choice.")]
    [MinMax(0f, 150f, 1f)]
    public float ShotFoliageFadeStart = 30f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover choice")]
    [Name("Bush Fade End Distance")]
    [Description("A bush this far (or farther) from the shot area and out of its line of sight has no penalty: a good ambush spot. Related: Works with the other settings in Shot Reaction: Cover > Cover choice.")]
    [MinMax(10f, 300f, 1f)]
    public float ShotFoliageFadeEnd = 90f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover search")]
    [Name("Wide Cover Search")]
    [Description("While a bot is in a shot reaction (hiding, retreating, investigating) its cover search looks further around and further up and down, so a bot on a roof also sees cover inside and below the building. Off = the normal search box (70 x 70 m, 5 m up and down). Related: Needed for Search Radius and Search Height to apply; larger boxes cost more cover-finder time (see the performance log).")]
    public bool ShotCoverWideSearch = true;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover search")]
    [Name("Cover Search Radius")]
    [Description("Half width (meters) of the cover search box during a shot reaction (normal bots: 35). Related: Works with the other settings in Shot Reaction: Cover > Cover search.")]
    [MinMax(20f, 120f, 1f)]
    public float ShotCoverSearchRadius = 60f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover search")]
    [Name("Cover Search Height")]
    [Description("Half height (meters) of the cover search box during a shot reaction (normal bots: 5): how far up and down it looks for cover. Related: Works with the other settings in Shot Reaction: Cover > Cover search.")]
    [MinMax(5f, 40f, 1f)]
    public float ShotCoverSearchHeight = 15f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover search")]
    [Name("Cover From The Shot Area")]
    [Description("The cover finder only searches while the bot has a target with a known position. A bot that was only alerted (a Rogue told about a shot, a bot that lost its target after a retreat) has none, found no cover at all and fled blindly. On = during a shot reaction the finder uses the rough shot area as the threat position when the bot has no known position. Off = vanilla finder. Related: Lets a bot without a target position search for cover from the rough area (alerts and reports give it one).")]
    public bool ShotCoverFromArea = true;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover search")]
    [Name("Cover Search Interval In Cover")]
    [Description("Seconds between two cover searches for a bot in a shot reaction that already holds a cover (the normal interval is 0.1 s). Related: Slows the cover finder for bots already in cover; saves performance. The empty backoff slows it when nothing is found.")]
    [MinMax(0.1f, 5f, 10f)]
    public float ShotCoverHoldFindInterval = 0.5f;

    [DataMember]
    [Category("Shot Reaction: Cover/Cover search")]
    [Name("Cover Search Backoff Max")]
    [Description("A bot in a shot reaction whose search found no cover waits longer each time (0.2, 0.4, 0.8 ... s) up to this many seconds, instead of searching every 0.1 s. Related: Slows the cover finder after empty searches; works with Cover Search Interval In Cover.")]
    [MinMax(0.1f, 10f, 10f)]
    public float ShotCoverEmptyBackoffMax = 2f;

    [DataMember]
    [Category("Shot Reaction: Cover/Spreading out")]
    [Name("Cover: Max Bots Per Point")]
    [Description("A cover point that already has this many other bots in a shot reaction at it (or on their way to it, within Crowd Radius) is not offered to another one: no more piling into one spot. When every candidate is taken the bot's no-cover decision applies (fight or flee). 0 = no limit. Related: Together with Crowd Radius spreads bots over cover points instead of piling into one.")]
    [MinMax(0f, 10f, 1f)]
    public float ShotCoverMaxPerPoint = 2f;

    [DataMember]
    [Category("Shot Reaction: Cover/Spreading out")]
    [Name("Cover: Crowd Radius")]
    [Description("Radius (meters) around a cover point inside which other bots' cover counts as the same spot. Related: Defines 'the same spot' for Max Bots Per Point.")]
    [MinMax(1f, 30f, 1f)]
    public float ShotCoverCrowdRadius = 6f;

    [DataMember]
    [Category("Shot Reaction: Cover/Staying in cover")]
    [Name("Cover Time Min")]
    [Description("Shortest time, in seconds, a bot keeps to cover after choosing Cover or Suppress (for a close shooter). Related: Cut short by Hide Leave Delay: a bot in cover may leave the hide that many seconds after arriving, whatever this says. Raise Hide Leave Delay for this setting to matter.")]
    [MinMax(0.5f, 30f, 10f)]
    public float ShotCoverTimeMin = 3f;

    [DataMember]
    [Category("Shot Reaction: Cover/Staying in cover")]
    [Name("Cover Time Max")]
    [Description("Longest time, in seconds, a bot keeps to cover after choosing Cover or Suppress (for a shooter at the edge of its reach or beyond). Related: Cut short by Hide Leave Delay (see Cover Time Min).")]
    [MinMax(0.5f, 60f, 10f)]
    public float ShotCoverTimeMax = 10f;

    [DataMember]
    [Category("Shot Reaction: Cover/Staying in cover")]
    [Name("Hide Leave Delay After Arriving (s)")]
    [Description("After a bot that was shot at has reached cover, it can leave the hide this many seconds later, whatever the hide time (Cover Time Min/Max) says. Raise it for bots that stay in cover longer. Was a fixed 1.5 s. Related: Overrides Cover Time Min/Max once the bot is in cover. Separate from Keep Cover Point, which stops it from swapping cover points.")]
    [MinMax(0.5f, 30f, 10f)]
    public float ShotHideLeaveDelay = 4f;

    [DataMember]
    [Category("Shot Reaction: Cover/Staying in cover")]
    [Name("Keep Cover Point (s)")]
    [Description("After a bot reaches its cover during a shot reaction it keeps it at least this many seconds, even when the next shot moves the guessed shooter position and marks the cover as bad (no running around between props). 0 = off. Related: Not the time spent hiding (see Hide Leave Delay and Cover Time Min/Max): this only stops a bot from changing cover point after it arrived.")]
    [MinMax(0f, 15f, 10f)]
    public float ShotCoverHold = 4f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Follow-up pick")]
    [Name("Follow-up: Fight On")]
    [Description("Weight: after hiding, the bot stays in cover and fights back (suppressive fire at the vague area, normal combat if it sees the shooter). Personality and weapon reach scale the weights. Related: Compared with the other two weights, then modified by squad roles, injury, reach, uncertainty (Investigate Floor), personality and Far Range Fight On Boost.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowFightOn = 0.35f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Follow-up pick")]
    [Name("Follow-up: Investigate")]
    [Description("Weight: after hiding, the bot goes looking for the shooter (normal search toward the vague area, stops and fights when it sees the shooter). Related: Compared with the other follow-up weights; reduced by uncertainty and injury, raised by squadmates fighting and by aggressive personality.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowInvestigate = 0.4f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Follow-up pick")]
    [Name("Follow-up: Retreat")]
    [Description("Weight: after hiding, the bot relocates away from the shot area (see the Relocate settings). Ignored if Relocate is off. Related: Compared with the other follow-up weights; ignored when Shot Relocate After Hiding is off. Uses Retreat and Retreat route settings.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotFollowRetreat = 0.25f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Follow-up pick")]
    [Name("Fight-On Duration")]
    [Description("Seconds the bot keeps fighting back from cover after hiding, before it may forget the shooter. Related: How long Fight On lasts; the Fight On burst and pause settings act inside it.")]
    [MinMax(2f, 60f, 10f)]
    public float ShotFightOnDuration = 15f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat")]
    [Name("Shot Relocate After Hiding")]
    [Description("After a bot that hid from an unseen shooter (Cover or Suppress outcome) is done hiding, it walks away to a different area instead of returning to where it was shot at, then resumes its task from there. Related: Only switches the follow-up Retreat pick. The duel and pressure retreats have their own switches and also use the distances and route below.")]
    public bool ShotRelocateEnabled = true;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat")]
    [Name("Shot Relocate Min Distance")]
    [Description("Shortest distance in meters of the new area from where the bot hid. Related: The base retreat distance of every retreat: after hiding, the duel (times Duel Leave Distance Multiplier) and the Rogue pressure retreat (times Pressure Retreat Distance Multiplier).")]
    [MinMax(10f, 150f, 1f)]
    public float ShotRelocateMinDistance = 40f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat")]
    [Name("Shot Relocate Max Distance")]
    [Description("Longest distance in meters of the new area from where the bot hid. Related: See Min Distance: same base for all retreats.")]
    [MinMax(10f, 250f, 1f)]
    public float ShotRelocateMaxDistance = 80f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat")]
    [Name("Shot Relocate Time Limit")]
    [Description("The bot gives up relocating and goes back to normal behavior after this many seconds. Related: Caps the walk of every retreat: after hiding, duel, pressure.")]
    [MinMax(10f, 180f, 1f)]
    public float ShotRelocateTimeMax = 60f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Route: Direct Weight")]
    [Description("Weight of the straight run to the retreat point. Related: The route weights apply to every retreat (after hiding, duel, pressure); Exposed Line Limit lowers Direct.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteDirect = 25f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Route: Bend Weight")]
    [Description("Weight of a run with one sideways waypoint. Related: Works with the other settings in Shot Reaction: After Hiding > Retreat route.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteBend = 35f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Route: Zig-Zag Weight")]
    [Description("Weight of a run with two waypoints on alternating sides. Related: Works with the other settings in Shot Reaction: After Hiding > Retreat route.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteZigZag = 20f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Route: Cover First Weight")]
    [Description("Weight of a run that first goes to the nearest point the shot area cannot see. Related: Works with the other settings in Shot Reaction: After Hiding > Retreat route.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotRouteCoverFirst = 20f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Exposed Line Limit")]
    [Description("When more than this share of the straight line is visible from the shot area, the Direct weight drops to 5 and Cover First and Zig-Zag gain weight. Related: Works with the other settings in Shot Reaction: After Hiding > Retreat route.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRouteExposureLimit = 0.5f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Waypoint Offset Min")]
    [Description("Smallest sideways offset (meters) of a bend or zig-zag waypoint. Related: Works with the other settings in Shot Reaction: After Hiding > Retreat route.")]
    [MinMax(2f, 40f, 1f)]
    public float ShotRouteOffsetMin = 8f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Waypoint Offset Max")]
    [Description("Largest sideways offset (meters) of a bend or zig-zag waypoint. Related: Works with the other settings in Shot Reaction: After Hiding > Retreat route.")]
    [MinMax(4f, 60f, 1f)]
    public float ShotRouteOffsetMax = 20f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Retreat Squad Weight")]
    [Description("Score bonus for retreat points close to a squadmate (full bonus next to a mate, none at the Retreat Squad Range). A retreating bot falls back toward its group. 0 = ignore the squad. Related: Prefers retreat points near squadmates in all retreats; Retreat Squad Range sets how near.")]
    [MinMax(0f, 3f, 100f)]
    public float ShotRetreatSquadWeight = 0.5f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Retreat route")]
    [Name("Retreat Squad Range")]
    [Description("Distance (meters) from the nearest squadmate beyond which a retreat point gets no squad bonus. Related: Range of the squad bonus of Retreat Squad Weight.")]
    [MinMax(10f, 200f, 1f)]
    public float ShotRetreatSquadRange = 60f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Safety: Quiet Time")]
    [Description("Seconds without being shot at or seeing the shooter until the quiet part of the safety level is full. Related: Quiet part of the safety level; the hold after any retreat (including duel and pressure) ends once safety reaches the safe-enough value.")]
    [MinMax(5f, 180f, 1f)]
    public float ShotSafetyQuietTime = 40f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Safety: Distance Start")]
    [Description("Distance from the shot area where the distance part of the safety level starts to count. Related: Works with the other settings in Shot Reaction: After Hiding > Hold after retreat.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotSafetyDistStart = 20f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Safety: Distance Full")]
    [Description("Distance from the shot area where the distance part of the safety level is full. Related: Works with the other settings in Shot Reaction: After Hiding > Hold after retreat.")]
    [MinMax(20f, 300f, 1f)]
    public float ShotSafetyDistFull = 100f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Safe Enough: Min")]
    [Description("After a retreat the bot holds until its safety level reaches a random value between Min and Max (aggressive bots lower, timid bots higher). Related: Random safe-enough value between Min and Max; timid personalities add, aggressive ones subtract.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotSafetyThresholdMin = 0.6f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Safe Enough: Max")]
    [Description("Upper end of the random safe-enough level. Related: Works with the other settings in Shot Reaction: After Hiding > Hold after retreat.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotSafetyThresholdMax = 0.9f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Hold After Retreat: Minimum")]
    [Description("The bot holds at least this many seconds after a retreat, whatever its safety level. Related: Floor of the hold after retreat. Duel and pressure retreats raise it to Long Retreat Min Hold.")]
    [MinMax(0f, 60f, 1f)]
    public float ShotPostRetreatMinHold = 10f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Hold After Retreat: Longest, Lower Bound")]
    [Description("Upper limit of the hold after a retreat is a random value between this and the next setting (scaled by personality). Related: Hold After Retreat: the longest hold is random between this and the Upper Bound; Long Retreat Min Hold can raise it.")]
    [MinMax(5f, 180f, 1f)]
    public float ShotPostRetreatHoldMin = 30f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Hold After Retreat: Longest, Upper Bound")]
    [Description("Upper end of the longest hold after a retreat. Related: Upper bound of the longest hold after a retreat.")]
    [MinMax(5f, 300f, 1f)]
    public float ShotPostRetreatHoldMax = 60f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Long Retreat Min Hold (s)")]
    [Description("After a duel retreat or a Rogue pressure retreat the bot stays away at least this long (holding cover) before it may feel safe and investigate again. Related: Applies to every bot after a duel leave or a Rogue pressure retreat: the hold lasts at least this long, whatever the safety level, and the longest hold is raised to it.")]
    [MinMax(0f, 300f, 1f)]
    public float ShotLongRetreatMinHold = 45f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Stay Mobile Below Safety")]
    [Description("While the safety level is below this, a holding bot moves to another cover every few seconds instead of sitting still. Related: Below this safety level a holding bot moves to another cover every Mobile Hop interval.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotMobileSafety = 0.5f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Mobile Hop Interval: Min")]
    [Description("Shortest time between cover moves while the bot does not feel safe. Related: Works with the other settings in Shot Reaction: After Hiding > Hold after retreat.")]
    [MinMax(2f, 60f, 1f)]
    public float ShotMobileHopMin = 6f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Hold after retreat")]
    [Name("Mobile Hop Interval: Max")]
    [Description("Longest time between cover moves while the bot does not feel safe. Related: Works with the other settings in Shot Reaction: After Hiding > Hold after retreat.")]
    [MinMax(2f, 60f, 1f)]
    public float ShotMobileHopMax = 12f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Hunt Time Max")]
    [Description("A bot that chose to hunt gives up after this many seconds from the shot if it has not seen the shooter. Related: Ends an investigation; also the time the hunt after the hold lasts.")]
    [MinMax(5f, 180f, 1f)]
    public float ShotHuntTimeMax = 90f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Cover Hold: Min")]
    [Description("While investigating, the bot holds each cover at least this many seconds (personality scaled). Related: Also the hold time of the duel push hops (cover to cover toward you).")]
    [MinMax(0f, 30f, 1f)]
    public float ShotHopHoldMin = 3f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Cover Hold: Max")]
    [Description("The longest hold in each cover while investigating. Related: Also the hold time of the duel push hops.")]
    [MinMax(0f, 60f, 1f)]
    public float ShotHopHoldMax = 7f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Hop: No Progress Time")]
    [Description("An investigation hop is abandoned when the bot has not gotten closer to its cover for this many seconds. Related: Also abandons a duel push hop that makes no progress.")]
    [MinMax(1f, 30f, 10f)]
    public float ShotHopNoProgressTime = 5f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Hop: Longest Time")]
    [Description("The longest an investigation hop may take (seconds), however much progress it makes. Related: Also the longest duel push hop.")]
    [MinMax(10f, 120f, 1f)]
    public float ShotHopMaxTime = 40f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Ambush Hold: Min")]
    [Description("When the bot gets close to the shot area or hears the shooter it holds an ambush this many seconds at least. Related: Works with the other settings in Shot Reaction: After Hiding > Investigate.")]
    [MinMax(0f, 120f, 1f)]
    public float ShotAmbushMin = 20f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Ambush Hold: Max")]
    [Description("Longest ambush hold. Related: Works with the other settings in Shot Reaction: After Hiding > Investigate.")]
    [MinMax(0f, 240f, 1f)]
    public float ShotAmbushMax = 40f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Ambush Distance")]
    [Description("The bot stops advancing and holds when it is this close (meters) to the shot area. Related: Works with the other settings in Shot Reaction: After Hiding > Investigate.")]
    [MinMax(0f, 100f, 1f)]
    public float ShotInvestigateStopDistance = 15f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Advance Without Cover Chance")]
    [Description("A bot that investigates, sits in cover with no closer cover point and is still farther than the Ambush Distance from the shot area runs toward the area (instead of holding and watching) with this chance. Aggressive personalities advance more often, timid ones less. 0 = always hold. Related: Scaled by uncertainty and personality; works with Advance Without Cover Time.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotAdvanceNoCoverChance = 0.6f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Advance Without Cover Time")]
    [Description("Longest time (seconds) of that run. After it the bot holds and watches wherever it is. Related: Also the sprint length of a duel push when no closer cover exists.")]
    [MinMax(3f, 40f, 1f)]
    public float ShotAdvanceNoCoverTime = 12f;

    [DataMember]
    [Category("Shot Reaction: After Hiding/Investigate")]
    [Name("Sprinting Search After Hunt")]
    [Description("After a hunt runs out with the shooter unseen, the bot searches at a run for this many seconds. 0 = off. Related: Works with the other settings in Shot Reaction: After Hiding > Investigate.")]
    [MinMax(0f, 300f, 1f)]
    public float ShotReturnWindow = 60f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Another retreat")]
    [Name("Another Retreat: Hit Chance")]
    [Description("Chance that a hit during a retreat or the hold after it starts another retreat (personality scaled). Related: Another retreat when hit again during a retreat or its hold; limited by Max Count, Interval and Min Time Since Retreat Start.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatHitChance = 0.6f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Another retreat")]
    [Name("Another Retreat: Bullet Chance")]
    [Description("The same for a bullet passing close. Related: Works with the other settings in Shot Reaction: Shot Again > Another retreat.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatBulletChance = 0.25f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Another retreat")]
    [Name("Startled Bonus")]
    [Description("Added to the chance when the bot had just felt safe (safety at or above the startled level). Related: Works with the other settings in Shot Reaction: Shot Again > Another retreat.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatStartledBonus = 0.3f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Another retreat")]
    [Name("Startled Safety Level")]
    [Description("A bot whose safety level was at least this when shot counts as startled (and retreats 25 percent farther). Related: Works with the other settings in Shot Reaction: Shot Again > Another retreat.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRepeatStartledSafety = 0.6f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Another retreat")]
    [Name("Another Retreat: Max Count")]
    [Description("Most extra retreats per reaction chain. Related: Cap of the repeat retreats per chain; Interval and Min Time Since Retreat Start limit how fast.")]
    [MinMax(0f, 10f, 1f)]
    public int ShotRepeatMax = 2;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Another retreat")]
    [Name("Another Retreat: Interval")]
    [Description("Minimum seconds between extra retreats. Related: Works with the other settings in Shot Reaction: Shot Again > Another retreat.")]
    [MinMax(0f, 60f, 1f)]
    public float ShotRepeatInterval = 8f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Another retreat")]
    [Name("Another Retreat: Min Time Since Retreat Start")]
    [Description("A repeat retreat can only start this many seconds after the previous retreat began, so a second hit right at the start does not restart it. Related: Works with the other settings in Shot Reaction: Shot Again > Another retreat.")]
    [MinMax(0f, 20f, 10f)]
    public float ShotRepeatMinRetreatTime = 3f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Self preservation")]
    [Name("Near-Hit Distance (m)")]
    [Description("A bullet passing within this many meters of a bot counts as a close bullet for the shot reaction (alerts, self preservation, duel and pressure retreats), for every bot with the shot reaction on, Rogues and Goons included. Stock SAIN uses about 2 m. The game only reports bullets within 10 m. A Rogue on a mounted gun keeps its own wider range. A close bullet at a Rogue or Goon also counts for the Rogues and Goons within the Narrowing Shared Radius. Related: Counts a passing bullet as a close bullet for everything: shots again, self preservation, the duel (hits and close bullets), the Rogue pressure retreat and Rogue radio call-ins. Gunners use their own range (Gunners: leaving the gun).")]
    [MinMax(0f, 10f, 1f)]
    public float ShotNearHitDistance = 5f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Self preservation")]
    [Name("Move On Hit")]
    [Description("Chance that a bot hit while sheltering moves to another cover (personality scaled). Not at close range, never while fighting. Related: Chance to move to another cover on a hit while sheltering; Move Interval limits how often.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSelfPreserveHit = 0.9f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Self preservation")]
    [Name("Move On Bullet")]
    [Description("The same for a bullet passing close (a bot in a bush uses the hit chance). Related: Chance on a close bullet; the stack settings below add to it (up to the Stack Ceiling).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSelfPreserveBullet = 0.2f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Self preservation")]
    [Name("Move Interval")]
    [Description("Minimum seconds between such cover moves. Related: Works with the other settings in Shot Reaction: Shot Again > Self preservation.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotSelfPreserveInterval = 3f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Self preservation")]
    [Name("Close Bullet Stack Step")]
    [Description("Every close bullet within the stack window adds this much to the chance (0.2 = 20% for the first bullet, 22% for the second with 0.02) that a bot in cover moves to another cover, up to the ceiling. A move resets it. Related: Each close bullet adds this to the Move On Bullet chance until the Ceiling; resets after the Stack Reset.")]
    [MinMax(0f, 0.2f, 100f)]
    public float ShotSelfPreserveBulletStep = 0.02f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Self preservation")]
    [Name("Close Bullet Stack Ceiling")]
    [Description("The most the stacked close-bullet chance can reach, so suppression alone does not keep a bot moving. Related: Upper limit of the stacked Move On Bullet chance (see Close Bullet Stack Step).")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSelfPreserveBulletCeiling = 0.5f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Self preservation")]
    [Name("Close Bullet Stack Reset (s)")]
    [Description("Seconds without a close bullet after which the stack starts again from the first bullet. Related: Resets the Close Bullet Stack after this long without a close bullet.")]
    [MinMax(1f, 120f, 1f)]
    public float ShotBulletStackReset = 20f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Break investigation")]
    [Name("Break Investigation: Hit Chance")]
    [Description("Chance that a hit while investigating makes the bot run for cover and reassess. Related: A hit while investigating breaks it; Lockout limits how often.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotInvestigateBreakHit = 1f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Break investigation")]
    [Name("Break Investigation: Bullet Chance")]
    [Description("The same for a bullet passing close. Related: Works with the other settings in Shot Reaction: Shot Again > Break investigation.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotInvestigateBreakBullet = 0.2f;

    [DataMember]
    [Category("Shot Reaction: Shot Again/Break investigation")]
    [Name("Break Investigation: Lockout")]
    [Description("After an investigation was broken by fire, no further break for this many seconds. Related: Works with the other settings in Shot Reaction: Shot Again > Break investigation.")]
    [MinMax(0f, 120f, 1f)]
    public float ShotBreakLockout = 15f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Squad")]
    [Name("Squad Roles Enabled")]
    [Description("The follow-up pick after a hide takes squadmates into account: a mate that fights (Fight, Fight On) makes the others lean to investigate (flank), a mate that investigates makes the others lean to fight on from cover (cover fire). Off = every bot picks on its own. Related: Works with the other settings in Shot Reaction: Squad and Grenades > Squad.")]
    public bool ShotSquadRolesEnabled = true;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Squad")]
    [Name("Squad Role Range")]
    [Description("Squadmates farther than this (meters) do not count for the squad roles and the squad alert. Related: Range for squad roles and for the squad alert; the duel resolve counts squadmates within a fixed 50 m.")]
    [MinMax(10f, 150f, 1f)]
    public float ShotSquadRoleRange = 40f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Squad")]
    [Name("Squad Role Strength")]
    [Description("Added to the lean per qualifying squadmate (0.4 = 40% more weight on the complementary pick), capped at +100%. Related: Works with the other settings in Shot Reaction: Squad and Grenades > Squad.")]
    [MinMax(0f, 2f, 100f)]
    public float ShotSquadRoleStrength = 0.4f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Squad")]
    [Name("Squad Alert Enabled")]
    [Description("When a bot is hit by a shooter it cannot see, squadmates within the Squad Role Range that know the shooter and are not already reacting get the vague shot area and roll their own reaction as for a close bullet. Off = only the hit bot reacts (SAIN still shares the heard position). Related: Works with the other settings in Shot Reaction: Squad and Grenades > Squad.")]
    public bool ShotSquadAlertEnabled = true;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Squad")]
    [Name("Squad Alert Delay Min")]
    [Description("Minimum seconds before a squadmate reacts to a squad alert (a hit or kill of a squadmate), so it does not snap into the reaction at once. Related: Also the delay of ordinary squad sightings (Rough area rings: Sightings Shared As Rough Points).")]
    [MinMax(0f, 10f, 10f)]
    public float ShotSquadAlertDelayMin = 0.5f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Squad")]
    [Name("Squad Alert Delay Max")]
    [Description("Maximum seconds before a squadmate reacts to a squad alert. Related: Also the delay of ordinary squad sightings.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotSquadAlertDelayMax = 2f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenades Enabled")]
    [Description("A bot that holds cover after a hide may throw a grenade at the vague shot area first (before it retreats, fights on from cover, investigates, or starts an ambush). Uses SAIN's own throw logic and its safety checks (not while running, friendlies close, range, needs a grenade). Off = the reaction never throws. Related: Master switch of the grenade throws at the vague area; the chances below belong to the follow-up and ambush; the uncertainty limit (Uncertainty effects) also applies.")]
    public bool ShotGrenadeEnabled = true;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenade Chance: Retreat")]
    [Description("Chance of a grenade at the end of a hide that continues as a retreat (cover, nade, run). Related: Works with the other settings in Shot Reaction: Squad and Grenades > Grenades.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceRetreat = 0.5f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenade Chance: Fight On")]
    [Description("Chance of a grenade at the end of a hide that continues as Fight On (suppression from cover). Related: Works with the other settings in Shot Reaction: Squad and Grenades > Grenades.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceFightOn = 0.35f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenade Chance: Investigate")]
    [Description("Chance of a grenade at the end of a hide that continues as an investigation. Related: Works with the other settings in Shot Reaction: Squad and Grenades > Grenades.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceInvestigate = 0.3f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenade Chance: Ambush")]
    [Description("Chance of a grenade when an investigating bot has no closer cover and settles into its ambush. Related: Works with the other settings in Shot Reaction: Squad and Grenades > Grenades.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotGrenadeChanceAmbush = 0.3f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenade Min Distance")]
    [Description("No grenade at a vague area closer than this (meters). Related: Works with the other settings in Shot Reaction: Squad and Grenades > Grenades.")]
    [MinMax(5f, 60f, 1f)]
    public float ShotGrenadeMinDistance = 20f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenade Max Distance")]
    [Description("No grenade at a vague area farther than this (meters). Related: Works with the other settings in Shot Reaction: Squad and Grenades > Grenades.")]
    [MinMax(20f, 100f, 1f)]
    public float ShotGrenadeMaxDistance = 60f;

    [DataMember]
    [Category("Shot Reaction: Squad and Grenades/Grenades")]
    [Name("Grenade Cooldown")]
    [Description("Seconds before the same bot may throw again from a shot reaction. Related: Works with the other settings in Shot Reaction: Squad and Grenades > Grenades.")]
    [MinMax(5f, 300f, 1f)]
    public float ShotGrenadeCooldown = 45f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Retreat Enabled")]
    [Description("A bot that keeps firing at the player while seeing it for a while (see the times below) without hitting it once gives the position up (not at close range): a full retreat out of the player's sight, then the normal hold and investigation. Off = it keeps fighting. Related: Switches the whole duel (leave and push). Brain filter and Player Shooters Only still apply.")]
    public bool ShotDuelRetreatEnabled = true;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel: Firing At The Player For, Min (s)")]
    [Description("The shortest time (seconds) a bot keeps firing at the player while seeing it, without hitting it once, before it gives the position up. The clock resets on a hit by the bot, in the close-range zone and after Duel Forgotten After seconds without seeing the player and firing. Each bot picks a random time between Min and Max. Related: Base time; scaled by the bot's resolve (personality, reach vs distance, health, squadmates within 50 m) and cut by the pull, never under Time Floor.")]
    [MinMax(5f, 300f, 1f)]
    public float ShotDuelTimeMin = 20f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel: Firing At The Player For, Max (s)")]
    [Description("The longest time (seconds) of that duel. A hit by the bot on the player resets the clock. Related: Upper end of the random base time (see Time Min).")]
    [MinMax(5f, 300f, 1f)]
    public float ShotDuelTimeMax = 40f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Time Floor (s)")]
    [Description("The shortest the duel clock can get, however many misses or hits the bot has taken: it never gives up before this many seconds of dueling. Related: The pull and resolve can never push the clock under this.")]
    [MinMax(3f, 60f, 1f)]
    public float ShotDuelTimeFloor = 8f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel: Shots On It Needed")]
    [Description("Hits by the player on this bot, or bullets of the player passing close to it, within the Hit Window. 0 = the player does not have to shoot at it at all (a bot that keeps missing the player retreats on its own). Related: 0 = you do not have to shoot at the bot for it to give up. Counts hits and close bullets (Near-Hit Distance) in the Hit Window.")]
    [MinMax(0f, 10f, 1f)]
    public float ShotDuelShots = 0f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel: Hit Window (s)")]
    [Description("The hits and close bullets have to fall within this many seconds, and the bot must not have hit the player in that time. Related: Window for the hits and close bullets that count here and in the pull (Pull Per Hit).")]
    [MinMax(10f, 600f, 1f)]
    public float ShotDuelWindow = 60f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel: Cooldown (s)")]
    [Description("A bot that has made a duel retreat does not make another for this many seconds. Related: A repeat inside Repeat Window also goes further (Leave).")]
    [MinMax(0f, 1200f, 1f)]
    public float ShotDuelCooldown = 120f;

    [DataMember]
    [Category("Duel/Misses and hits (the pull)")]
    [Name("Duel Burst Gap (s)")]
    [Description("Trigger pulls closer together than this belong to one burst. A whole burst counts as one miss, however many bullets it has. Related: Defines a burst for the pull: a whole burst counts as one miss.")]
    [MinMax(0.3f, 5f, 10f)]
    public float ShotDuelBurstGap = 1.5f;

    [DataMember]
    [Category("Duel/Misses and hits (the pull)")]
    [Name("Duel Miss Window (s)")]
    [Description("Only bursts that missed inside this window count toward the pull to leave. A hit by the bot on the player clears them. Related: Only misses in this window count; a hit by the bot on you clears them.")]
    [MinMax(5f, 120f, 1f)]
    public float ShotDuelMissWindow = 15f;

    [DataMember]
    [Category("Duel/Misses and hits (the pull)")]
    [Name("Duel Pull Per Missed Burst")]
    [Description("Each missed burst cuts this share off the duel time (0.12 = 12%), and makes the retreat go further. Pull is capped at 80%. Related: Share cut off the duel time and added to the leave distance, per missed burst; capped (total pull 0.8).")]
    [MinMax(0f, 0.5f, 100f)]
    public float ShotDuelMissWeight = 0.12f;

    [DataMember]
    [Category("Duel/Misses and hits (the pull)")]
    [Name("Duel Pull Per Hit On The Bot")]
    [Description("Each hit or close bullet of the player on the bot inside the Hit Window cuts this share off the duel time. Related: Share cut off the duel time per hit or close bullet on the bot in the Hit Window; counts into the same pull.")]
    [MinMax(0f, 0.5f, 100f)]
    public float ShotDuelHitWeight = 0.1f;

    [DataMember]
    [Category("Duel/Push")]
    [Name("Duel Push Resolve")]
    [Description("A bot whose resolve (personality, reach against the distance, squad nearby, health) is at least this and that is not being pulled away does not retreat when the clock ends: it pushes, cover to cover toward the player (the investigate rules). 1 = never. Related: Resolve is personality, reach vs distance, health and squadmates within 50 m. A push also needs pull under 0.3 and you inside the bot's reach. Lower = more bots push.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotDuelPushResolve = 0.65f;

    [DataMember]
    [Category("Duel/Push")]
    [Name("Duel Push Time (s)")]
    [Description("The longest a bot pushes toward the player (cover to cover) before it goes back to normal combat. Related: Longest push; ends earlier at Stops At, close range, no closer cover or Aborts At Pull.")]
    [MinMax(5f, 180f, 1f)]
    public float ShotDuelPushTime = 30f;

    [DataMember]
    [Category("Duel/Push")]
    [Name("Duel Push Stops At (share of reach)")]
    [Description("The push ends when the player is inside this share of the bot's weapon reach (0.6 = 60%): it is close enough to hit and fights from there. Related: Push ends when you are inside this share of the bot's reach.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotDuelPushStopReach = 0.6f;

    [DataMember]
    [Category("Duel/Push")]
    [Name("Duel Push Aborts At Pull")]
    [Description("A bot that pushes and takes this much pull (missed bursts, hits on it) is losing: the push ends and it retreats instead. Related: A pushing bot whose pull reaches this retreats instead (Leave).")]
    [MinMax(0.1f, 0.8f, 100f)]
    public float ShotDuelPushAbortPull = 0.5f;

    [DataMember]
    [Category("Duel/Leave")]
    [Name("Duel Leave Distance Multiplier")]
    [Description("A bot that gives up a duel (it insisted on shooting without hitting) relocates this many times the normal relocate distance (Relocate Min/Max). 1.5 = 60 to 120 m with the default 40 to 80 m. When no navmesh point exists that far the retreat falls back to 60%, 35% and 20% of the distance (never under 15 m). Related: Times the base retreat distance (Retreat: Min/Max Distance): stubborn bots go shorter, timid or beaten ones further; the pull adds up to 50%.")]
    [MinMax(1f, 6f, 10f)]
    public float ShotDuelDistanceMult = 1.5f;

    [DataMember]
    [Category("Duel/Leave")]
    [Name("Duel Leave Distance Multiplier Max")]
    [Description("Every repeat of a duel retreat inside the Repeat Window adds 0.5 to the multiplier, up to this value: a bot that keeps coming back to shoot retreats further each time. Related: Cap including the +0.5 per repeat inside the Repeat Window.")]
    [MinMax(1f, 10f, 10f)]
    public float ShotDuelDistanceMultMax = 3f;

    [DataMember]
    [Category("Duel/Leave")]
    [Name("Duel Repeat Window (s)")]
    [Description("A duel retreat within this many seconds of the last one counts as a repeat (longer retreat distance). Related: A duel retreat inside this window of the last one counts as a repeat (+0.5 distance multiplier).")]
    [MinMax(30f, 1200f, 1f)]
    public float ShotDuelRepeatWindow = 300f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Rogue Radio: Delayed Call-In")]
    [Description("On = a Rogue that is hit or has a bullet pass close does not react at once: after a short wait it runs for cover and radios the others, both at the same moment. A Rogue that dies during the wait makes no call (the kill rules below apply). Also removes the vanilla instant hit broadcast. Off = react at once, vanilla broadcast limited by Rogue Alert Range and Chance. Related: Delays the Rogue's own reaction and its alert; the hit and bullet waits below set the length, the alert delays and chance set the other Rogues' reaction.")]
    public bool RogueCallInDelayed = true;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Hit Call-In Delay Min")]
    [Description("Shortest wait (seconds) between a hit on a Rogue and its cover run and radio call. Related: The alerted Rogues react after this plus their own Rogue Alert delay.")]
    [MinMax(0f, 5f, 100f)]
    public float RogueHitCallInMin = 0.1f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Hit Call-In Delay Max")]
    [Description("Longest wait (seconds) between a hit on a Rogue and its cover run and radio call. Related: Works with the other settings in Rogue Behaviour > Radio: call-ins and alerts.")]
    [MinMax(0f, 5f, 100f)]
    public float RogueHitCallInMax = 1f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Bullet Call-In Delay Min")]
    [Description("Shortest wait (seconds) between a bullet passing close to a Rogue and its cover run and radio call. Related: The alerted Rogues react after this plus their own Rogue Alert delay.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueFlyByCallInMin = 1f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Bullet Call-In Delay Max")]
    [Description("Longest wait (seconds) between a bullet passing close to a Rogue and its cover run and radio call. If the bullet kills the Rogue, nothing is sent from the bullet. Related: Works with the other settings in Rogue Behaviour > Radio: call-ins and alerts.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueFlyByCallInMax = 2f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Rogue Alert Enabled")]
    [Description("When a Rogue is hit by, or a bullet passes close to it from, a shooter it cannot see, the other Rogues within the Rogue Alert Range get the vague shot area and take Cover (staggered by up to Rogue Alert Delay). A Rogue on a mounted gun whose sector covers that area stays on the gun. Needs Mounted Gun Abandon Enabled for the gun part. Related: Switches the alerts of hit, close-bullet, kill and man-down; Alert Range, Chance and the delays apply to all of them.")]
    public bool ShotRogueAlertEnabled = true;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Rogue Alert Range")]
    [Description("Rogues farther than this (meters) from the hit Rogue are not alerted. 0 = every Rogue on the map (the delay grows with the distance, see Rogue Alert Delay Per Meter). Also limits the vanilla hit broadcast. Related: Applies to hit, bullet, kill and man-down alerts; 0 = anywhere on the map.")]
    [MinMax(0f, 2000f, 1f)]
    public float ShotRogueAlertRange = 0f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Rogue Alert Chance")]
    [Description("Chance per Rogue within the Rogue Alert Range that it takes the alert (rolled once per Rogue per hit). 0 = nobody, 1 = every Rogue in range. Related: Per Rogue within Range, for every alert.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotRogueAlertChance = 1f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Rogue Alert Delay Min")]
    [Description("Minimum seconds before an alerted Rogue reacts, even the nearest one (reaction time). The alert delay is a random value from this to Rogue Alert Delay, plus the per-meter delay. Related: Floor of the alert delay for every alerted Rogue (reaction time); together with Delay Max and Per Meter.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotRogueAlertDelayMin = 2f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Rogue Alert Delay Max")]
    [Description("Each alerted Rogue reacts after a random delay from 0 to this many seconds, so they do not all move on the same frame. Related: Random delay of every alerted Rogue (0 to this) added to the Delay Per Meter; the alert delay for hits, bullets, kills and man-down.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotRogueAlertDelayMax = 8f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: call-ins and alerts")]
    [Name("Rogue Alert Delay Per Meter")]
    [Description("Extra seconds per meter between the hit Rogue and an alerted Rogue (0.01 = 5 s over 500 m), added to the random 0 to Rogue Alert Delay seconds. Related: Added per meter between the hit Rogue and the alerted one, to the random delay.")]
    [MinMax(0f, 0.1f, 1000f)]
    public float ShotRogueAlertDelayPerMeter = 0.02f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: kills")]
    [Name("Kill Needs A Witness")]
    [Description("On = the death of a Rogue is only radioed if another living Rogue could see it (within that Rogue's visible distance, inside its visible angle, clear line of sight to the body), after Witness Delay. Nobody saw it: no radio until the Man-Down Delay ends, then everybody covers with no position. Off = every kill alerts all Rogues at once. Related: Off = every kill is radioed after the Witness Delay; on = only if a living Rogue could see it, otherwise the Man-Down delay applies.")]
    public bool RogueKillNeedsWitness = true;

    [DataMember]
    [Category("Rogue Behaviour/Radio: kills")]
    [Name("Witness Delay Min")]
    [Description("Shortest wait (seconds) between a witnessed kill and the radio call. Related: On top of the alert delays (Radio: call-ins and alerts).")]
    [MinMax(0f, 10f, 10f)]
    public float RogueKillWitnessDelayMin = 2f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: kills")]
    [Name("Witness Delay Max")]
    [Description("Longest wait (seconds) between a witnessed kill and the radio call. Related: Works with the other settings in Rogue Behaviour > Radio: kills.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueKillWitnessDelayMax = 5f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: kills")]
    [Name("Man-Down Delay Min")]
    [Description("Shortest wait (seconds) before the Rogues notice a Rogue that died unseen. Then every Rogue takes cover from the body's area (a Rogue on a mounted gun leaves it), with no position of the shooter and no suppression. Related: An unwitnessed kill is only noticed after this; a real alert in the meantime cancels it.")]
    [MinMax(0f, 120f, 1f)]
    public float RogueManDownDelayMin = 20f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: kills")]
    [Name("Man-Down Delay Max")]
    [Description("Longest wait (seconds) before the Rogues notice a Rogue that died unseen. A real alert in the meantime cancels it. Related: Works with the other settings in Rogue Behaviour > Radio: kills.")]
    [MinMax(0f, 120f, 1f)]
    public float RogueManDownDelayMax = 40f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Rogues: No Zone Magic")]
    [Description("Vanilla tells every Rogue in an attack zone (water treatment plant) the exact position of any player who enters the zone and marks the player as visible to them, however far away and hidden. On = that is removed: Rogues must find the player by sight and sound. Off = vanilla. Related: Switches off vanilla's exact position for Rogues in an attack zone; the mounted gun and report settings still apply.")]
    public bool RogueNoZoneMagic = true;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Rogues: Limited Hit Broadcast")]
    [Description("Vanilla makes every Rogue on the map take the shooter of any hit Rogue as an enemy, whatever the distance. On = only Rogues within the Rogue Alert Range of the hit Rogue, each with the Rogue Alert Chance, take it (the Rogue itself always does). Off = vanilla. Related: Limits who takes the shooter of a hit Rogue as an enemy; the Alert settings then decide who reacts.")]
    public bool RogueLimitedBroadcast = true;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Rogue Reports: Delayed")]
    [Description("Vanilla gives every Rogue a spotting Rogue's report of the player (exact position) at once, at any distance. On = each Rogue gets it after a delay (min to max seconds plus travel time per meter), and only inside the report range. Related: Replaces the vanilla instant report: the delay settings, Range, Rough Points and Minimum Interval apply. Also applies to Goons.")]
    public bool RogueReportDelayed = true;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Rogue Reports: Delay Min")]
    [Description("Minimum seconds before a Rogue receives another Rogue's enemy report. Related: Works with the other settings in Rogue Behaviour > Radio: reports.")]
    [MinMax(0f, 30f, 10f)]
    public float RogueReportDelayMin = 2f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Rogue Reports: Delay Max")]
    [Description("Maximum base seconds before a Rogue receives another Rogue's enemy report. Related: Works with the other settings in Rogue Behaviour > Radio: reports.")]
    [MinMax(0f, 60f, 10f)]
    public float RogueReportDelayMax = 6f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Rogue Reports: Delay Per Meter")]
    [Description("Extra seconds per meter between the reporting Rogue and the receiving Rogue (0.01 = 5 s over 500 m). Related: Works with the other settings in Rogue Behaviour > Radio: reports.")]
    [MinMax(0f, 0.1f, 1000f)]
    public float RogueReportDelayPerMeter = 0.01f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Rogue Reports: Range")]
    [Description("Rogues farther than this (meters) from the reporting Rogue never receive its report. 0 = the whole map. Related: Works with the other settings in Rogue Behaviour > Radio: reports.")]
    [MinMax(0f, 2000f, 1f)]
    public float RogueReportRange = 0f;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Reports: Rough Points")]
    [Description("On = a report from another Rogue gives the receiver a point in a ring around the player, not the exact position: as rough as a shot from the receiver's own distance would be (concealment included), narrowed like every rough area. Off = the exact position, delayed (Report Delay). Related: The ring comes from the Rough area rings settings (Area Shrink etc.); off gives the exact position after the delay.")]
    public bool RogueReportRough = true;

    [DataMember]
    [Category("Rogue Behaviour/Radio: reports")]
    [Name("Report Minimum Interval")]
    [Description("A Rogue takes at most one report from the others per this many seconds; the vanilla report fires many times per second while a Rogue sees the player (hundreds of calls per second on the map). The rest are dropped before any work is done. 0 = no limit. Related: Limits how often a Rogue takes a report; reports in between are dropped.")]
    [MinMax(0f, 10f, 10f)]
    public float RogueReportMinInterval = 1f;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Rogue Pressure Retreat Enabled")]
    [Description("Rogues and Goons that keep getting shot at or near for a while (hits and bullets passing within the Near-Hit Distance, not shots they merely hear, at them or at Rogues and Goons within the Narrowing Shared Radius) without ever seeing the player give the position up: a full retreat, down off the roof. A window for the sniper, then they are gone. A hit or close bullet at one Rogue only keeps the pressure of its neighbours running; each Rogue has its own clock and its own random limit. A Rogue on a mounted gun leaves it. Off = they stay and keep covering. Related: Rogues and Goons only. Pressure comes from hits and close bullets (Near-Hit Distance) at the bot or at Rogues and Goons within Narrowing Shared Radius.")]
    public bool RoguePressureRetreatEnabled = true;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Pressure Time Min (s)")]
    [Description("Shortest time (seconds) of pressure before a Rogue or Goon retreats. Each bot picks a random time between Min and Max. Related: Random per bot between Min and Max; the clock only runs while the gap allows and the bot does not see you (then the Duel applies).")]
    [MinMax(10f, 600f, 1f)]
    public float RoguePressureTimeMin = 30f;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Pressure Time Max (s)")]
    [Description("Longest time (seconds) of pressure before a Rogue or Goon retreats. Related: Works with the other settings in Rogue Behaviour > Pressure retreat.")]
    [MinMax(10f, 600f, 1f)]
    public float RoguePressureTimeMax = 45f;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Pressure Gap (s)")]
    [Description("A quiet spell longer than this (no hit or close bullet at or near the bot) resets the pressure clock. A hit or close bullet keeps the clock running; it does not add to it. Related: A longer gap lets spaced shots keep building pressure; a shorter one resets sooner.")]
    [MinMax(5f, 120f, 1f)]
    public float RoguePressureGap = 30f;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Pressure Retreat Cooldown (s)")]
    [Description("A Rogue or Goon that made a pressure retreat does not make another for this many seconds. Related: After a pressure retreat; the hold afterwards is at least Long Retreat Min Hold.")]
    [MinMax(0f, 1800f, 1f)]
    public float RoguePressureCooldown = 120f;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Pressure Retreat Goes Down")]
    [Description("On = the retreat point has to be lower than the bot is now by at least Retreat Down Min (off the roof); if there is none it goes anywhere out of sight. Off = any retreat point. Related: Works with Retreat Down Min.")]
    public bool RogueRetreatDown = true;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Retreat Down Min (m)")]
    [Description("How much lower (meters) a retreat point has to be than the bot's position for a retreat that goes down. Related: Works with the other settings in Rogue Behaviour > Pressure retreat.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotRetreatDownMin = 5f;

    [DataMember]
    [Category("Rogue Behaviour/Pressure retreat")]
    [Name("Pressure Retreat Distance Multiplier")]
    [Description("A Rogue or Goon that runs (pressure retreat) goes this many times the normal relocate distance (Relocate Min/Max) away, falling back to 60%, 35% and 20% of it when no navmesh point exists that far. Related: Times the base retreat distance (Shot Relocate Min/Max Distance).")]
    [MinMax(1f, 6f, 10f)]
    public float RogueRetreatDistanceMult = 2.5f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: leaving the gun")]
    [Name("Mounted Gun Abandon Enabled")]
    [Description("A bot on (or running to) a mounted gun that is shot at by an unseen shooter checks the direction of the shot. Shooter inside the gun's firing sector: the bot stays and answers from the gun. Shooter outside the sector: it leaves the gun and reacts as any other bot (Cover or Fight roll). Off = vanilla behaviour (the gun layer outranks the reaction and the bot stays). Related: Works with the other settings in Rogue Behaviour > Gunners: leaving the gun.")]
    public bool ShotStationaryEnabled = true;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: leaving the gun")]
    [Name("Mounted Gun Hold Off Time")]
    [Description("Seconds a bot that left its mounted gun because of a shot is kept off it (vanilla would send it back after 45-60 s). Related: Also applies when a pressure retreat, an alert or a man-down makes the gunner leave its gun.")]
    [MinMax(0f, 1800f, 1f)]
    public float ShotStationaryHold = 180f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: leaving the gun")]
    [Name("Mounted Gun Answer Grace")]
    [Description("A gunner whose gun covers the shot stays on it only while it can answer. If it has not pulled the trigger (suppression counts) for this many seconds when the next hit, close bullet or alert reaches it, it leaves the post like a gunner the shot came outside of the arc for, and takes cover. 0 = it always stays when the shot is inside the arc. Related: A gunner stays on its gun only while it fired within this time.")]
    [MinMax(0f, 30f, 10f)]
    public float ShotStationaryAnswerGrace = 6f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: leaving the gun")]
    [Name("Mounted Close Bullet Distance")]
    [Description("A bullet that passes within this distance (meters) of a Rogue on a mounted gun counts as a close bullet for the shot reaction (normal bots: Max Under Fire Distance, about 2 m). 0 = same as other bots. Related: Gunners only; other Rogues use Near-Hit Distance (Shot Again > Self preservation).")]
    [MinMax(0f, 50f, 1f)]
    public float ShotMountedUnderFireDistance = 8f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: leaving the gun")]
    [Name("Mounted Impact Range")]
    [Description("A bullet of the player that lands within this distance (meters) of a Rogue on a mounted gun counts as a close bullet too (the Rogue sees where the shot landed). 0 = off. Related: Gunners only: a bullet impact this close counts as a close bullet (pressure, alerts).")]
    [MinMax(0f, 100f, 1f)]
    public float ShotMountedImpactRange = 12f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: aim and vision")]
    [Name("Mounted Guns: Known Position Only")]
    [Description("Vanilla mounted guns (machine guns, grenade launchers) aim and suppress at the player's TRUE live position even when the Rogue has not seen the player. On = they use the last position the Rogue actually knew, plus an error (Mounted Aim Error), and do not start suppression without knowing the player. Off = vanilla. Related: Mounted aim and suppression use the last known position; Aim Error sets the error; the launcher scatter and ranging in come on top.")]
    public bool RogueMountedKnownPosition = true;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: aim and vision")]
    [Name("Mounted Guns: Aim Error")]
    [Description("Error radius (meters) around the last known position that a mounted gun aims or suppresses at (re-rolled every 3 s). Related: Error radius around the known position; the launcher scatter adds to it.")]
    [MinMax(0f, 50f, 1f)]
    public float RogueMountedAimError = 8f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: aim and vision")]
    [Name("Mounted Guns: Spot Speed In Zone")]
    [Description("Vision speed multiplier for a Rogue on a mounted gun against a player inside the Rogue attack zone: 0.5 = twice as fast to spot, 1 = off. It only speeds up spotting, the player's position is not given away. Related: Works with the other settings in Rogue Behaviour > Gunners: aim and vision.")]
    [MinMax(0.1f, 1f, 100f)]
    public float RogueMountedSpotCoef = 0.5f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Suppression")]
    [Description("A Rogue on a mounted gun (grenade launcher or machine gun) that knows only a rough shot area (a hit on itself, a Rogue alert) and cannot see the player fires suppression bursts at that area, not at the player. Random: a chance roll, random burst length, random pauses, a jittering target. Off = it fires only at a player it knows. Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    public bool MountedAreaSuppress = true;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Chance")]
    [Description("Chance that an eligible gunner starts a suppression burst at each check (checks come every 1 to 1.5 s). A failed roll waits 2 to 5 s before the next. Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0f, 1f, 100f)]
    public float MountedAreaSuppressChance = 0.6f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Age")]
    [Description("The rough shot area counts only this many seconds after the last shot, hit or alert that refreshed it (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(5f, 120f, 1f)]
    public float MountedAreaSuppressAreaAge = 30f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Error")]
    [Description("Radius (meters) of the jitter around the rough area that the gun aims at, re-rolled every second. The area itself already has the shot position error on top. Related: Jitter radius of the suppression; for launchers it is multiplied by the ranging-in multiplier (Launcher scatter and ranging in).")]
    [MinMax(0f, 60f, 1f)]
    public float MountedAreaSuppressError = 12f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Grenade Burst Min")]
    [Description("Shortest suppression burst of a grenade launcher (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(1f, 30f, 10f)]
    public float MountedGrenadeBurstMin = 1f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Grenade Burst Max")]
    [Description("Longest suppression burst of a grenade launcher (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(1f, 30f, 10f)]
    public float MountedGrenadeBurstMax = 3f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted MG Burst Min")]
    [Description("Shortest suppression burst of a mounted machine gun (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0.5f, 20f, 10f)]
    public float MountedMgBurstMin = 1.5f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted MG Burst Max")]
    [Description("Longest suppression burst of a mounted machine gun (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0.5f, 20f, 10f)]
    public float MountedMgBurstMax = 4f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Pause Min")]
    [Description("Shortest pause between two suppression bursts of the same gunner (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0f, 120f, 10f)]
    public float MountedAreaCooldownMin = 6f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Pause Max")]
    [Description("Longest pause between two suppression bursts of the same gunner (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0f, 120f, 10f)]
    public float MountedAreaCooldownMax = 20f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Start Delay Min")]
    [Description("After a quiet spell (45 s without suppression) a gunner waits at least this long (seconds) after it first has a rough shot area before it starts suppressing, so it does not answer your first shot at once. Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0f, 60f, 10f)]
    public float MountedAreaStartDelayMin = 4f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Area Start Delay Max")]
    [Description("Longest wait before the first suppression burst after a quiet spell (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0f, 60f, 10f)]
    public float MountedAreaStartDelayMax = 8f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Grenade Pause Min")]
    [Description("Shortest pause between two suppression bursts of a grenade launcher (seconds). Machine guns use Mounted Area Pause. Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0f, 120f, 10f)]
    public float MountedGrenadePauseMin = 20f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Grenade Pause Max")]
    [Description("Longest pause between two suppression bursts of a grenade launcher (seconds). Related: Works with the other settings in Rogue Behaviour > Gunners: suppression.")]
    [MinMax(0f, 120f, 10f)]
    public float MountedGrenadePauseMax = 45f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Mounted Grenade Max Range")]
    [Description("A grenade launcher does not area-suppress, and does not count as able to answer, a rough area farther than this (meters) when it does not see its target: it takes cover instead. It still fires at a target it sees. 0 = no limit. Related: 0 = no limit; it only limits area suppression, not a target the gunner sees.")]
    [MinMax(0f, 2000f, 1f)]
    public float MountedGrenadeMaxRange = 0f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Grenade Launcher Max Uncertainty")]
    [Description("A Rogue on a grenade launcher area-suppresses only while the uncertainty of its rough shot area is at most this (meters). Above it, and without seeing its target, it cannot pinpoint the shooter: it leaves the post and takes cover. 0 = no limit. Related: Works with the uncertainty settings (Knowing Where The Shooter Is).")]
    [MinMax(0f, 500f, 1f)]
    public float MountedGrenadeMaxUncertainty = 20f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Machine Gun Max Uncertainty")]
    [Description("The same limit for a Rogue on a mounted machine gun. 0 = no limit. Related: Works with the uncertainty settings (Knowing Where The Shooter Is).")]
    [MinMax(0f, 500f, 1f)]
    public float MountedMgMaxUncertainty = 30f;

    [DataMember]
    [Category("Rogue Behaviour/Gunners: suppression")]
    [Name("Gunner Sighting Error Multiplier")]
    [Description("A gunner on a mounted gun (machine gun or grenade launcher) takes a sighting that another bot reported (a report, not a shot alert and not its own information) with this multiple of its error, for the two limits above: staying on the gun after an alert and starting a suppression burst. Mounted guns have no real scope. Applied when the gunner decides, not when the report arrives, so a bot that left its gun and retakes it is judged the same way. Repeated reports narrow the area, so a sustained sighting can still bring it under the limit. 1 = off. Related: Machine Gun and Grenade Launcher Max Uncertainty.")]
    [MinMax(1f, 10f, 10f)]
    public float MountedSightingErrorMultiplier = 2f;

    [DataMember]
    [Category("Rogue Behaviour/Launcher scatter and ranging in")]
    [Name("Grenade Launcher: Scatter")]
    [Description("Mounted grenade launchers (AGS) get extra aim error that grows with the distance to the target, so a launcher far away lands its grenades well off the player. Machine guns are not affected. Off = vanilla. Related: Applies to a launcher's aim at a seen or known target; Ranging In multiplies it; Max Uncertainty limits suppression.")]
    public bool GrenadeLauncherScatter = true;

    [DataMember]
    [Category("Rogue Behaviour/Launcher scatter and ranging in")]
    [Name("Grenade Launcher: Scatter Start Distance")]
    [Description("Distance (meters) from which the grenade launcher scatter starts to grow. Related: Works with the other settings in Rogue Behaviour > Launcher scatter and ranging in.")]
    [MinMax(0f, 500f, 1f)]
    public float GrenadeScatterStart = 100f;

    [DataMember]
    [Category("Rogue Behaviour/Launcher scatter and ranging in")]
    [Name("Grenade Launcher: Scatter Per Meter")]
    [Description("Scatter radius (meters at the target) added per meter beyond the start distance (0.05 = 25 m of radius at 600 m with a 100 m start). Related: Works with the other settings in Rogue Behaviour > Launcher scatter and ranging in.")]
    [MinMax(0f, 0.5f, 1000f)]
    public float GrenadeScatterPerMeter = 0.05f;

    [DataMember]
    [Category("Rogue Behaviour/Launcher scatter and ranging in")]
    [Name("Grenade Launcher: Scatter Max")]
    [Description("Largest scatter radius (meters at the target). Related: Works with the other settings in Rogue Behaviour > Launcher scatter and ranging in.")]
    [MinMax(0f, 200f, 1f)]
    public float GrenadeScatterMax = 60f;

    [DataMember]
    [Category("Rogue Behaviour/Launcher scatter and ranging in")]
    [Name("Launcher Ranging In: First Shot Multiplier")]
    [Description("A grenade launcher gunner ranges in: the scatter of its first grenade of an engagement (and the jitter of its first suppression burst) is this many times the normal value, 4 = four times. 1 = no ranging in. Related: Multiplies both the aim scatter and the suppression jitter of the first grenades; fades with every grenade fired.")]
    [MinMax(1f, 10f, 10f)]
    public float GrenadeRangingStart = 4f;

    [DataMember]
    [Category("Rogue Behaviour/Launcher scatter and ranging in")]
    [Name("Launcher Ranging In: Per Shot")]
    [Description("Every grenade the gunner fires (suppression bursts included, so it carries over from burst to burst) takes this share off the way from the first-shot multiplier down to 1 (0.2 = fully ranged in after 5 grenades). Related: Works with the other settings in Rogue Behaviour > Launcher scatter and ranging in.")]
    [MinMax(0.01f, 1f, 100f)]
    public float GrenadeRangingPerShot = 0.2f;

    [DataMember]
    [Category("Rogue Behaviour/Launcher scatter and ranging in")]
    [Name("Launcher Ranging In: Memory (s)")]
    [Description("Seconds without a grenade after which the gunner has forgotten its range and starts again with the wide first-shot scatter. Related: Works with the other settings in Rogue Behaviour > Launcher scatter and ranging in.")]
    [MinMax(5f, 600f, 1f)]
    public float GrenadeRangingMemory = 90f;

    [DataMember]
    [Category("Range/General")]
    [Name("Sight-Adjusted Reach")]
    [Description("A bot's effective reach (how far it stands and shoots) is its weapon class reach multiplied by a factor for its sights and scope zoom. Off = reach depends on the weapon class only (original behavior). Related: Works with the other settings in Range > General.")]
    public bool SightReachEnabled = true;

    [DataMember]
    [Category("Range/General")]
    [Name("Shoot Reach Factor")]
    [Description("A bot only aims and fires at a visible enemy closer than its effective reach times this value (includes distant-fire bursts). 0 = no limit (original behavior). The original stand-and-shoot rule already uses 1.25. Related: Works with the other settings in Range > General.")]
    [MinMax(0f, 3f, 100f)]
    public float ShootReachFactor = 1.25f;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: PMCs")]
    [Description("PMCs (USEC and BEAR) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainPmc = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Rogues")]
    [Description("Rogues (exUsec, Lighthouse) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainRogue = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Raiders")]
    [Description("Raiders (pmcBot: Labs, Reserve) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainRaider = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Scavs")]
    [Description("Regular scavs, scav groups and tagged-and-cursed scavs use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainScav = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Scav Snipers")]
    [Description("Scav snipers (marksman) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainScavSniper = false;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Cultists")]
    [Description("Cultists use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainCultist = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Bosses")]
    [Description("Bosses (not the Goons) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainBoss = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Boss Followers")]
    [Description("Boss guards (not the Goons) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainFollower = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Goons")]
    [Description("Knight, Big Pipe and Birdeye use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainGoon = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Event Bots")]
    [Description("Event bots (crazy scav event, gifter) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainEvent = true;

    [DataMember]
    [Category("Range/Brain filter")]
    [Name("Brain: Other Bots")]
    [Description("Any bot type not listed above (zombies, BTR, arena fighters, Labyrinth bots, ...) use the fork's range behavior (sight-adjusted reach, shoot and suppress limits, over-reach fire). Related: Applies to all Range settings (reach, suppress limit, over-reach); the Shot Reaction brain filter is separate.")]
    public bool RangeBrainOther = false;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Iron Sights Reach Factor")]
    [Description("Reach multiplier for a weapon with no optic and no red dot. 0.6 turns a 150 m assault rifle into a 90 m weapon. Related: Works with the other settings in Range > Sights.")]
    [MinMax(0.1f, 2f, 100f)]
    public float IronSightReach = 0.6f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Red Dot Reach Factor")]
    [Description("Reach multiplier for a weapon with a red dot or holographic sight (no magnified optic). Related: Works with the other settings in Range > Sights.")]
    [MinMax(0.1f, 2f, 100f)]
    public float RedDotReach = 0.75f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Optic Reach Factor At 1x")]
    [Description("Reach multiplier for an optic at 1x zoom. Every extra x of zoom adds Optic Reach Per Zoom. Related: Works with the other settings in Range > Sights.")]
    [MinMax(0.1f, 2f, 100f)]
    public float OpticReachAt1x = 0.8f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Optic Reach Per Zoom")]
    [Description("Added to the optic reach factor for every x of zoom above 1. 0.1 gives a 4x scope a factor of 1.1 and an 8x scope 1.5. Related: Works with the other settings in Range > Sights.")]
    [MinMax(0f, 0.5f, 100f)]
    public float OpticReachPerZoom = 0.1f;

    [DataMember]
    [Category("Range/Sights")]
    [Name("Optic Reach Factor Max")]
    [Description("The highest reach multiplier a scope can give. Related: Works with the other settings in Range > Sights.")]
    [MinMax(0.5f, 4f, 100f)]
    public float OpticReachMax = 1.6f;

    [DataMember]
    [Category("Range/Suppress limit")]
    [Name("Suppress Limit")]
    [Description("Bots do not fire suppressing bursts at a last known position farther away than their effective reach times Suppress Reach Fraction. Off = no limit (original behavior). Related: Works with the other settings in Range > Suppress limit.")]
    public bool SuppressLimitEnabled = true;

    [DataMember]
    [Category("Range/Suppress limit")]
    [Name("Suppress Reach Fraction")]
    [Description("Maximum suppress distance as a fraction of the bot's effective reach. 0.8 of a 150 m reach is 120 m. Related: Works with the other settings in Range > Suppress limit.")]
    [MinMax(0.1f, 2f, 100f)]
    public float SuppressReachFraction = 0.8f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Fire")]
    [Description("A bot that sees an enemy beyond its shooting reach (reach times Shoot Reach Factor) still gets a small chance to shoot, falling with distance. Off = a hard cut-off at that distance. Related: Works with the other settings in Range > Over-reach fire.")]
    public bool OverReachEnabled = true;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Chance")]
    [Description("Chance, rolled once per interval, that the bot fires at an enemy just past its reach. Falls linearly to 0 at the limit below. Related: Works with the other settings in Range > Over-reach fire.")]
    [MinMax(0f, 1f, 100f)]
    public float OverReachChance = 0.5f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Limit Factor")]
    [Description("Beyond this multiple of the bot's reach it never fires. Must be above Shoot Reach Factor. Related: Works with the other settings in Range > Over-reach fire.")]
    [MinMax(1f, 5f, 100f)]
    public float OverReachLimitFactor = 4f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Roll Interval")]
    [Description("Seconds between over-reach rolls for a bot. Longer = fewer, rarer bursts. Related: Works with the other settings in Range > Over-reach fire.")]
    [MinMax(1f, 30f, 1f)]
    public float OverReachRollInterval = 6f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Window")]
    [Description("A successful roll lets the bot fire for about this many seconds (varies by a third either way). Related: Works with the other settings in Range > Over-reach fire.")]
    [MinMax(0.5f, 15f, 10f)]
    public float OverReachWindow = 3f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Scatter")]
    [Description("Shots beyond the bot's reach scatter this many times more, unless the bot has had you in continuous view for the time below. 1 = no extra scatter. Related: Works with the other settings in Range > Over-reach fire.")]
    [MinMax(1f, 10f, 10f)]
    public float OverReachScatterMulti = 4f;

    [DataMember]
    [Category("Range/Over-reach fire")]
    [Name("Over-Reach Accurate After")]
    [Description("Seconds of continuous view of the enemy after which an over-reach shot is as accurate as normal (the bot has really tracked you). Related: Works with the other settings in Range > Over-reach fire.")]
    [MinMax(0f, 30f, 10f)]
    public float OverReachAccurateAfter = 5f;

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Random Suppress Cutoff")]
    [Description("Each magazine, a bot stops suppressing your last known position at a different ammo level, so going quiet does not reliably mean it is reloading. Off = every bot stops at the Fixed Suppress Cutoff. Related: Works with the other settings in Reload > Suppress cutoff.")]
    public bool RandomSuppressCutoff = true;

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Suppress Cutoff Min")]
    [Description("Lowest ammo ratio (0-1) at which a bot stops suppressing. 0.25 = it can fire down to a quarter magazine. Related: Works with the other settings in Reload > Suppress cutoff.")]
    [MinMax(0f, 1f, 100f)]
    public float SuppressCutoffMin = 0.25f;

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Suppress Cutoff Max")]
    [Description("Highest ammo ratio (0-1) at which a bot stops suppressing. Higher = some bots go quiet while still mostly loaded. Related: Works with the other settings in Reload > Suppress cutoff.")]
    [MinMax(0f, 1f, 100f)]
    public float SuppressCutoffMax = 0.65f;

    [DataMember]
    [Category("Reload/Suppress cutoff")]
    [Name("Fixed Suppress Cutoff")]
    [Description("Ammo ratio (0-1) at which bots stop suppressing when Random Suppress Cutoff is off. Original SAIN value: 0.33. Related: Works with the other settings in Reload > Suppress cutoff.")]
    [MinMax(0f, 1f, 100f)]
    public float FixedSuppressCutoff = 0.33f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Before Reloading")]
    [Description("When a bot starts reloading in close quarters, it leaves its cover and reloads somewhere else instead of staying where you saw it shooting. Off = original behavior. Related: Works with the other settings in Reload > Relocate before reload.")]
    public bool RelocateEnabled = true;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Chance")]
    [Description("Chance (0-1) that a bot relocates when the conditions are met. Below 1 keeps it from being a pattern. Related: Works with the other settings in Reload > Relocate before reload.")]
    [MinMax(0f, 1f, 100f)]
    public float RelocateChance = 0.6f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Max Enemy Distance")]
    [Description("Only relocate if the enemy is closer than this many meters (close quarters). Related: Works with the other settings in Reload > Relocate before reload.")]
    [MinMax(1f, 150f, 1f)]
    public float RelocateMaxEnemyDistance = 25f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Max Time Since Seen")]
    [Description("Only relocate if the bot saw the enemy within this many seconds, or was shot at by it. Related: Works with the other settings in Reload > Relocate before reload.")]
    [MinMax(1f, 120f, 1f)]
    public float RelocateMaxTimeSinceSeen = 15f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Cooldown")]
    [Description("Minimum seconds between relocations for one bot, to stop it running back and forth. Related: Works with the other settings in Reload > Relocate before reload.")]
    [MinMax(0f, 120f, 1f)]
    public float RelocateCooldown = 8f;

    [DataMember]
    [Category("Reload/Relocate before reload")]
    [Name("Reload Relocate Min Distance From Old Cover")]
    [Description("The new cover point must be at least this many meters from the one the bot left. Related: Works with the other settings in Reload > Relocate before reload.")]
    [MinMax(0f, 30f, 10f)]
    public float RelocateMinDistanceFromOldCover = 3f;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Bots Close Doors")]
    [Description("Bots close doors that are already open when one is in their way. Off by default: bots then leave open doors alone instead of closing the door they just opened. Related: Works with the other settings in Doors > Closing.")]
    public bool BotsCloseDoors = false;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Close Door When Stuck")]
    [Description("A bot that has stopped making progress next to an open door closes it, which frees a bot wedged against the open leaf. Independent of Bots Close Doors. Related: Works with the other settings in Doors > Closing.")]
    public bool StuckCloseDoor = true;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Stuck Close Door Delay")]
    [Description("Seconds without progress before a stuck bot closes a nearby open door. Related: Works with the other settings in Doors > Closing.")]
    [MinMax(0.1f, 10f, 10f)]
    public float StuckCloseDoorDelay = 1f;

    [DataMember]
    [Category("Doors/Closing")]
    [Name("Stuck Close Door Range")]
    [Description("A stuck bot only closes open doors within this many meters. Related: Works with the other settings in Doors > Closing.")]
    [MinMax(0.5f, 6f, 10f)]
    public float StuckCloseDoorRange = 2.5f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Back Away From Door Swing")]
    [Description("When a bot pulls a door open, it walks to a point outside the swing of the door instead of staying where the door hits it. Off = original behavior. Related: Works with the other settings in Doors > Swing & interaction.")]
    public bool DoorRetreatEnabled = true;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Swing Margin")]
    [Description("Extra meters beyond the door's swing circle the bot backs away to. Related: Works with the other settings in Doors > Swing & interaction.")]
    [MinMax(0f, 3f, 10f)]
    public float DoorRetreatMargin = 0.7f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Retreat Max Distance")]
    [Description("The most a bot will back away from a door, in meters. Related: Works with the other settings in Doors > Swing & interaction.")]
    [MinMax(0.5f, 8f, 10f)]
    public float DoorRetreatMaxDistance = 3f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Retreat Speed")]
    [Description("How fast a bot backs away from a door, in m/s. Low on purpose: backing up is slower than walking forward. Related: Works with the other settings in Doors > Swing & interaction.")]
    [MinMax(0.2f, 4f, 10f)]
    public float DoorRetreatSpeed = 1f;

    [DataMember]
    [Category("Doors/Swing & interaction")]
    [Name("Door Interaction Max Time")]
    [Description("The longest a door interaction (including backing away) can hold the bot, in seconds. Related: Works with the other settings in Doors > Swing & interaction.")]
    [MinMax(1f, 10f, 10f)]
    public float DoorInteractionMaxTime = 3f;

    [DataMember]
    [Category("Duel/Misses and hits (the pull)")]
    [Name("Duel Weight: Marksman Rifle")]
    [Description("A missed burst counts this many times for a bot with a marksman rifle (SVD, SVDS, DMRs), by rifle class and not by fire mode. Fades in with the distance as a share of the bot's reach (1x up close, see Long Range Start/Full). Related: multiplies the Pull Per Missed Burst and Pull Per Hit On The Bot above; the total pull is still capped at 0.8.")]
    [MinMax(1f, 6f, 10f)]
    public float ShotDuelWeightMarksman = 1.4f;

    [DataMember]
    [Category("Duel/Misses and hits (the pull)")]
    [Name("Duel Weight: Sniper Rifle")]
    [Description("A missed burst counts this many times for a bot with a sniper rifle (bolt actions like the M700). Fades in with the distance as a share of reach. Related: same as the marksman weight; a hit on a sniper also rolls the sniper rule (Long Rifles).")]
    [MinMax(1f, 6f, 10f)]
    public float ShotDuelWeightSniper = 1.8f;

    [DataMember]
    [Category("Duel/Long rifles")]
    [Name("Spotted Marksman/Sniper: Chance Per Hit")]
    [Description("A bot with a marksman or sniper rifle that sees you and is firing at you (a running duel) and is hit rolls this chance (0 to 1) to give the position up at once (a full leave, after a pause in its fire). No chance inside the Long Range Start (a share of its reach), full strength from Long Range Full. Scaled by the long range fade, by its resolve (personality, equipment, health, squad: timid leaves more, stubborn less) and raised a little by every earlier hit or close bullet in the same duel, so it often takes two or three. Related: Close Bullet chance, Long Range Start/Full, Duel Leave settings, Duel Flavor.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotDuelSniperHitChance = 0.35f;

    [DataMember]
    [Category("Duel/Long rifles")]
    [Name("Spotted Marksman/Sniper: Chance Per Close Bullet")]
    [Description("The same roll, only during a running duel, for a bullet passing within the Near-Hit Distance (Shot Again > Self preservation), without a hit. Related: Chance Per Hit, Near-Hit Distance, Long Range Start/Full.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotDuelSniperCloseChance = 0.15f;

    [DataMember]
    [Category("Duel/Long range")]
    [Name("Long Range Start (share of reach)")]
    [Description("The duel's long range rules start at this share of the bot's weapon reach (0.4 = 40%): below it the duel acts as before. Related: Long Range Full, Long Range End, Weapon weights, the sniper rule and the squad loss rule all fade in from here.")]
    [MinMax(0.1f, 2f, 100f)]
    public float ShotDuelLongStart = 0.4f;

    [DataMember]
    [Category("Duel/Long range")]
    [Name("Long Range Full (share of reach)")]
    [Description("At this share of reach the weapon weights and the sniper rule count fully. Related: Long Range Start.")]
    [MinMax(0.2f, 3f, 100f)]
    public float ShotDuelLongFull = 1f;

    [DataMember]
    [Category("Duel/Long range")]
    [Name("Long Range End (share of reach)")]
    [Description("At this share of reach the duel time has shrunk to Long Range Min Time Factor. Related: Duel Time Min/Max, Time Floor.")]
    [MinMax(0.3f, 4f, 100f)]
    public float ShotDuelLongEnd = 1.5f;

    [DataMember]
    [Category("Duel/Long range")]
    [Name("Long Range Min Time Factor")]
    [Description("The duel time at Long Range End is this share of the normal time (0.5 = half). Never below the Time Floor.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotDuelLongMinFactor = 0.5f;

    [DataMember]
    [Category("Duel/Long range")]
    [Name("Short Range Time Factor")]
    [Description("Up to the Long Range Start the duel time is stretched: at point blank it is this many times longer, falling to 1x at the Long Range Start, and the Time Floor stretches with it. So mid and short fights almost never end with a bot turning to leave mid-fight. Related: Duel Time Min/Max, Time Floor, Long Range Start.")]
    [MinMax(1f, 10f, 10f)]
    public float ShotDuelShortTimeFactor = 3f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Waits For A Pause In Fire (s)")]
    [Description("A bot that has decided to leave or push never starts while it is firing a burst: it waits for a pause in its fire (longer than the Burst Gap), at most this many seconds. Related: Burst Gap (Misses and hits), Duel Cooldown.")]
    [MinMax(0f, 20f, 10f)]
    public float ShotDuelWaitPause = 6f;

    [DataMember]
    [Category("Duel/Squad loss")]
    [Name("Squad Loss: Resolve Penalty")]
    [Description("When a squadmate is killed by you, the survivors lose this much resolve (timid ones 30% more, aggressive ones 30% less) for the Squad Loss Time, and may decide at once (a 40% to 85% chance by resolve, only while their duel clock runs: they see you and fire): leave, or push if their resolve and numbers allow it. In the close-range zone a separate roll applies (Squad Loss: Close Range Factor). 0 = off. Related: Duel Push Resolve, Squad Role Range (who counts as a squadmate), Squad Loss Min Distance.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotDuelSquadLossPenalty = 0.3f;

    [DataMember]
    [Category("Duel/Squad loss")]
    [Name("Squad Loss: Time (s)")]
    [Description("How long the resolve penalty after a squad kill lasts.")]
    [MinMax(5f, 300f, 1f)]
    public float ShotDuelSquadLossTime = 60f;

    [DataMember]
    [Category("Duel/Squad loss")]
    [Name("Squad Loss: Min Distance (share of reach)")]
    [Description("The squad loss rule only applies when you are at least this share of the survivor's reach away. 0 = at every range outside the close-range zone, where there is no duel and the fight stays a normal close fight. It also applies to a push in progress: a squadmate killed during it may break the push. Related: Long Range Start, Duel Push Resolve.")]
    [MinMax(0f, 2f, 100f)]
    public float ShotDuelSquadLossMinShare = 0f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Flavor (randomness)")]
    [Description("Each engagement the bot's resolve gets a random offset of up to plus or minus this value, and the push or leave outcome flips with a chance of 0.6 times this value (0.08 = about 5%), so duels are never fully predictable. 0 = no randomness. Related: Duel Push Resolve, the sniper rule, the squad loss rule.")]
    [MinMax(0f, 0.3f, 100f)]
    public float ShotDuelFlavor = 0.08f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Log")]
    [Description("Writes one log line when a duel clock starts and every 15 s while it runs (time seen firing, limit, resolve, pull, weapon, distance, why it is not leaving yet). For tuning. Related: Shot Reaction Log.")]
    public bool ShotDuelLog = true;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Break Off Chance")]
    [Description("A bot that is badly injured or dying and who sees you (or is being hit), at any range, rolls this chance to break off at once and retreat to heal (out of your sight: far at long range, a short way at close range, see Hurt Close Distance). Scaled by how hurt it is (dying 100%, badly injured 70%), by its resolve (timid, alone or outranged leaves more; aggressive with a squad less) and by Hurt: No Meds Factor. Rolled every Hurt Roll Interval while it stays hurt. Related: Hurt Close Factor, Hurt Close Distance, Hurt Avoid Distance, Hurt Push Chance, Hurt Distance, Hurt Min Hold, Duel Waits For A Pause In Fire.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotHurtLeaveChance = 0.6f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: No Meds Factor")]
    [Description("Multiplies the Break Off Chance when the bot has nothing to heal with (no first aid or surgical kit it would use). Below 1 a hurt bot without meds is less eager to leave, since there is nothing to heal. Related: Hurt: Break Off Chance.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotHurtNoMedsFactor = 0.6f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Push Anyway Chance")]
    [Description("When the Break Off roll fails, the chance that the hurt bot goes for the push instead (cover to cover toward you, as in a duel push), if you are inside its weapon reach and its pull is low. Otherwise it keeps fighting. Related: Duel Push settings, Hurt: Break Off Chance.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotHurtPushChance = 0.3f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Distance Multiplier")]
    [Description("A hurt bot's retreat distance is at least this many times the normal relocate distance (Shot Relocate Min/Max Distance). Falls back to shorter distances when the navmesh has no point that far. Related: Duel Leave Distance Multiplier, Shot Relocate Min/Max Distance.")]
    [MinMax(1f, 8f, 10f)]
    public float ShotHurtDistanceMult = 2.5f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Min Hold (s)")]
    [Description("After a hurt retreat the bot holds cover at least this long, so SAIN's own healing has time to run before it investigates again. Related: Long Retreat Min Hold, Hold After Retreat.")]
    [MinMax(0f, 300f, 1f)]
    public float ShotHurtMinHold = 60f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Roll Interval (s)")]
    [Description("How often a bot that stays badly hurt rolls again (Break Off Chance, then Push Anyway). Related: Hurt: Break Off Chance.")]
    [MinMax(5f, 120f, 1f)]
    public float ShotHurtRollInterval = 30f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Close Range Factor")]
    [Description("Multiplies the Break Off Chance at close range, where running means turning your back on the shooter. 1 = same as at range, 0 = a hurt bot never breaks off at close range. Related: Hurt: Break Off Chance, Hurt Close Distance, Hurt Avoid Distance.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotHurtCloseFactor = 0.8f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Close Range Distance Multiplier")]
    [Description("At close range a hurt bot only goes this many times the normal relocate distance (a short way, to the nearest place out of your sight to heal), not the long retreat of Hurt Distance Multiplier. Related: Shot Relocate Min/Max Distance, Hurt Distance Multiplier.")]
    [MinMax(0.2f, 4f, 10f)]
    public float ShotHurtCloseDistanceMult = 0.6f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Avoid Distance (m)")]
    [Description("At close range the retreat path must not pass within this many meters of you. If no point can be reached that way the bot is trapped (the only way out leads through you) and stays and fights instead. 0 = no check. Related: Hurt: Close Range Factor.")]
    [MinMax(0f, 15f, 10f)]
    public float ShotHurtAvoidDistance = 4f;

    [DataMember]
    [Category("Duel/Squad loss")]
    [Name("Squad Loss: Close Range Factor")]
    [Description("At close range (where the duel does not run) a survivor of a squad kill may still break off: the chance is the squad loss chance (0.4 to 0.85 by resolve) times this. 1 = same as farther away, 0 = never at close range. It only starts in a pause in its fire and needs a way out that does not pass you (Hurt: Avoid Distance), else it stays and fights. Bots without squadmates are not affected. Related: Squad Loss Resolve Penalty, Hurt: Close Range Distance Multiplier, Hurt: Avoid Distance, Duel Waits For A Pause In Fire.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSquadLossCloseFactor = 0.7f;

    [DataMember]
    [Category("Duel/Hurt")]
    [Name("Hurt: Close Range Retreat Needs Hiding")]
    [Description("On = at close range a break-off (hurt, or after a squad kill) only goes to a point out of your sight; with none, or with no way out that does not pass you, the bot stays and fights (no running into the open). Off = any reachable point. Costs one navmesh path and line check per candidate, once per break-off. Related: Hurt: Avoid Distance, Hurt: Close Range Factor, Squad Loss: Close Range Factor.")]
    public bool ShotHurtCloseNeedsHidden = true;

    [DataMember]
    [Category("Duel/Retreat planning")]
    [Name("Known Position Window (s)")]
    [Description("A bot knows where you are while it sees you and for this many seconds after it last saw you (the position is then where it last saw you). A bot that knows retreats against that position and only to a point out of your sight; a bot that does not know keeps using the rough area. Related: Retreat Needs Hiding, Hurt: Close Range Retreat Needs Hiding, the Rough area rings.")]
    [MinMax(1f, 60f, 1f)]
    public float ShotKnownPositionWindow = 8f;

    [DataMember]
    [Category("Duel/Retreat planning")]
    [Name("Known Position: Retreat Needs Hiding")]
    [Description("On = a bot that knows where you are (duel leave, hurt and squad-death break-offs) beyond close range also ONLY retreats to a point out of your sight, and stays and fights if there is none. Off (default) = beyond close range a point out of your sight is preferred but a retreat in your sight is allowed, since at mid and long range that is viable. At close range Hurt: Close Range Retreat Needs Hiding applies either way. Related: Known Position Window, Hurt: Avoid Distance, Duel Leave settings.")]
    public bool ShotKnownRetreatNeedsHidden = false;

    [DataMember]
    [Category("Duel/Squad follow")]
    [Name("Squad Follow: Leave Chance")]
    [Description("When a bot of an ordinary squad or a Rogue squad starts a duel leave, a hurt break-off or a squad-death break-off, each squadmate in the same fight (within the Squad Role Range and engaged) rolls this chance to leave too, scaled by its own resolve (low resolve follows more), halved if the leaver was only hurt and 1.4x after a squad death. It uses its own retreat plan after a short delay, and never pulls anyone else along. Related: Squad Follow Max Share, Squad Follow Delay, Squad Role Range, Duel Leave settings.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSquadFollowLeaveChance = 0.35f;

    [DataMember]
    [Category("Duel/Squad follow")]
    [Name("Squad Follow: Push Chance")]
    [Description("When a bot starts a duel push, each squadmate that could push itself (inside its reach, not at close range, low pull) rolls this chance to join the push, scaled by its resolve (high resolve joins more). The mates that do not join keep shooting. Related: Duel Push settings, Squad Follow Leave Chance.")]
    [MinMax(0f, 1f, 100f)]
    public float ShotSquadFollowPushChance = 0.2f;

    [DataMember]
    [Category("Duel/Squad follow")]
    [Name("Squad Follow: Max Share Leaving")]
    [Description("A squad leave pulls at most this share of the squad (0.5 = half, rounded), and always leaves at least one bot in the fight. After a squad death this cap does not apply. Related: Squad Follow Leave Chance.")]
    [MinMax(0.1f, 1f, 100f)]
    public float ShotSquadFollowMaxShare = 0.5f;

    [DataMember]
    [Category("Duel/Squad follow")]
    [Name("Squad Follow: Delay Min (s)")]
    [Description("A follower starts its retreat or push this many seconds at the earliest after its squadmate (random between Min and Max), so a squad does not move in the same second. It still waits for a pause in its fire. Related: Duel Waits For A Pause In Fire.")]
    [MinMax(0f, 10f, 10f)]
    public float ShotSquadFollowDelayMin = 1f;

    [DataMember]
    [Category("Duel/Squad follow")]
    [Name("Squad Follow: Delay Max (s)")]
    [Description("The latest start of a follower after its squadmate.")]
    [MinMax(0f, 15f, 10f)]
    public float ShotSquadFollowDelayMax = 4f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Forgotten After (s)")]
    [Description("A bot that stops seeing you and firing at you for this long is no longer dueling: the clock and its pressure reset. A bot that sees you but does not fire resets too. Related: Duel Forgotten After (Sniper/Marksman), Time Min/Max, Duel Cooldown.")]
    [MinMax(3f, 120f, 1f)]
    public float ShotDuelResetTime = 15f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Forgotten After (Sniper/Marksman) (s)")]
    [Description("The same for a bot with a marksman or sniper rifle, which fires slowly: a longer downtime between shots does not end the duel. Never shorter than Duel Forgotten After. Related: Duel Weight settings, Duel Forgotten After.")]
    [MinMax(3f, 180f, 1f)]
    public float ShotDuelResetTimeLongRifle = 30f;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Ticks In Combat")]
    [Description("The duel clock also runs when the bot is in combat with the player without firing at him: the player hit it or put a bullet close to it in the last 10 s while it sees him or saw him in the last 8 s, or it sees him but cannot hit him (out of weapon reach and not firing; that runs faster, see Duel Cannot-Reach Speed). Only while the shot reaction is not managing the bot, so the two never overlap. A bot that never saw the player stays with the shot reaction alone. Off = the clock only runs while the bot sees the player and fires. Related: Needs the duel retreat switched on; the limit, resolve and pull settings apply as before.")]
    public bool ShotDuelTicksInCombat = true;

    [DataMember]
    [Category("Duel/Clock")]
    [Name("Duel Cannot-Reach Speed")]
    [Description("How much faster the duel clock runs while the bot sees the player, is out of weapon reach and is not firing (1 = normal speed). Related: Duel Ticks In Combat.")]
    [MinMax(1f, 4f, 10f)]
    public float ShotDuelCantReachSpeed = 1.5f;

    [DataMember]
    [Category("Combat Cover/Bushes")]
    [Name("Bush Ban In Combat")]
    [Description("A bush hides a bot but stops nothing, and it is only a surprise while the player has not met the bot. With this on, a bot does not pick a bush as cover once the player has met it (it saw the player within Bush Ban Memory) or shot at or near it within that time. If only bushes are left it has no cover and falls back to the normal no-cover behaviour (a shot reaction decides fight or flight). Off = bushes are only penalised, as before. Related: Bush Ban Memory, Bush In Player View; the Shot Reaction: Cover > Cover choice bush settings (path penalty, fade distances) still apply to bushes that are allowed.")]
    public bool CombatBushBan = true;

    [DataMember]
    [Category("Combat Cover/Bushes")]
    [Name("Bush Ban Memory (s)")]
    [Description("How long after the bot last saw the player, or was shot at or near by them, bushes stay banned for it. Related: Bush Ban In Combat.")]
    [MinMax(0f, 120f, 1f)]
    public float CombatBushMemory = 10f;

    [DataMember]
    [Category("Combat Cover/Bushes")]
    [Name("Bush In Player View")]
    [Description("A bush the player is looking toward (within about 50 degrees of where they aim) with a clear line to it counts as watched and is not picked, even if the player has not spotted the bot. A bush behind the player or behind a wall is still allowed. Only against a player enemy. Related: Needs Bush Ban In Combat; works with Bush Ban Memory.")]
    public bool CombatBushView = true;

    [DataMember]
    [Category("Combat Cover/Leaving a bush")]
    [Name("Leave Bush At Close Range")]
    [Description("A bot in a bush that is shot at or near by the player at close range leaves for other cover instead of holding and firing back: the bush stops nothing. Without this, the close-range rule of the shot reaction (hold cover and fire back) also keeps a bot in a bush. Related: Part of the self-preservation move of the shot reaction (Shot Reaction: Cover); needs Shot Reaction on. Skipped when no other cover exists.")]
    public bool CombatBushLeaveClose = true;

    [DataMember]
    [Category("Combat Cover/Seasons")]
    [Name("No Bushes In Summer")]
    [Description("Never use bushes as cover in a Summer raid (any bot, any situation, including the shot reaction). A bot already in one leaves. Related: The other seasons below; bushes in the other situations are handled by Bush Ban In Combat.")]
    public bool NoBushesSummer = false;

    [DataMember]
    [Category("Combat Cover/Seasons")]
    [Name("No Bushes In Autumn")]
    [Description("Never use bushes as cover in an Autumn raid (the foliage is thin). A bot already in one leaves. Related: The other seasons.")]
    public bool NoBushesAutumn = true;

    [DataMember]
    [Category("Combat Cover/Seasons")]
    [Name("No Bushes In Late Autumn")]
    [Description("Never use bushes as cover in a Late Autumn raid. A bot already in one leaves. Related: The other seasons.")]
    public bool NoBushesAutumnLate = true;

    [DataMember]
    [Category("Combat Cover/Seasons")]
    [Name("No Bushes In Winter")]
    [Description("Never use bushes as cover in a Winter raid (the foliage is gone). A bot already in one leaves. Related: The other seasons.")]
    public bool NoBushesWinter = true;

    [DataMember]
    [Category("Combat Cover/Seasons")]
    [Name("No Bushes In Early Spring")]
    [Description("Never use bushes as cover in an Early Spring raid (the foliage is still bare). A bot already in one leaves. Related: The other seasons.")]
    public bool NoBushesSpringEarly = true;

    [DataMember]
    [Category("Combat Cover/Seasons")]
    [Name("No Bushes In Spring")]
    [Description("Never use bushes as cover in a Spring raid. A bot already in one leaves. Related: The other seasons.")]
    public bool NoBushesSpring = false;

    [DataMember]
    [Category("Combat Cover/Stalled retreat")]
    [Name("Retreat Stall Time (s)")]
    [Description("A bot on a retreat (the flight after a shot, a duel retreat or a pressure retreat) that has moved less than Min Progress for this long, and is not at its target, ends the retreat as stalled: it takes the nearest cover and holds it, as after an arrival, and normal combat takes over. 0 = off (a stuck retreat runs until Shot Relocate Time Max). Related: Retreat Stall Min Progress; logged as 'relocation over: stalled'.")]
    [MinMax(0f, 30f, 1f)]
    public float ShotRetreatStallTime = 6f;

    [DataMember]
    [Category("Combat Cover/Stalled retreat")]
    [Name("Retreat Stall Min Progress (m)")]
    [Description("How far (meters) a retreating bot must move within Retreat Stall Time to count as making progress. Related: Retreat Stall Time.")]
    [MinMax(0.5f, 10f, 10f)]
    public float ShotRetreatStallMinProgress = 2f;

    public override void Init(List<ISAINSettings> list)
    {
        list.Add(this);
    }
}
