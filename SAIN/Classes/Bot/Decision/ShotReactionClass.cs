using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.Patches.Generic;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings.Categories;
using SAIN.Preset.Shared.Models.Preset.Personalities;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Decision;

public enum EShotReaction
{
    None,
    Cover,
    Fight,
    FightOn,
    Hunt,
}

/// <summary>
/// Fork: what a bot does when it is shot at by an enemy it cannot see.
/// A hit or a close bullet gives the bot a vague area (never the exact position) and one weighted roll:
/// Cover (hide, then forget), Fight (no hiding: normal combat at once, against the vague area), Hunt (hide, assess, then look for the shooter); after a hide Fight On keeps the bot in cover firing at the area,
/// The Hunt state is the investigation that follows a hide. The roll is event driven: nothing here runs per tick.
/// </summary>
public class ShotReactionClass : BotComponentClassBase
{
    private const float HIT_INFO_BONUS = 1.3f;
    private const float SPRINT_COVER_BONUS = 1.3f;
    private const float SPRINT_FIGHT_PENALTY = 0.5f;
    private const float INJURED_COVER_BONUS = 1.5f;
    private const float INJURED_FIGHT_PENALTY = 0.5f;
    private const float FIGHT_REACH_LIMIT = 1.25f;

    // Distance smoothing: the zone tables are anchors on a curve over distance / reach, not steps.
    private const float LONG_ANCHOR_RATIO = 1.0f;

    /// <summary>Close-range limits (at or inside the Short anchor): Fight may reach this share and Cover may fall to this share.</summary>
    private const float CLOSE_FIGHT_MAX_SHARE = 0.85f;
    private const float CLOSE_COVER_MIN_SHARE = 0.10f;
    private const float FIGHT_FADE_START = 1.0f;
    private const float FIGHT_FADE_END = 1.5f;

    /// <summary>Suppress fades out between the suppress reach and this multiple of it.</summary>
    public const float SUPPRESS_FADE_STRETCH = 1.5f;

    private static readonly float[] RELOCATE_ANGLES = [0f, 35f, -35f, 70f, -70f, 110f, -110f];
    private const float RELOCATE_ARRIVED_DISTANCE = 3f;
    private const float VAGUE_POSITION_MAX_AGE = 20f;

    /// <summary>After this long in cover (instead of the full hide time) the bot leaves for its relocation point.</summary>
    private static float RELOCATE_COVER_HOLD => GlobalSettings.Fork.ShotHideLeaveDelay;

    /// <summary>The shooter only counts as seen once it has been continuously visible this long (a flicker of the vision flag must not cancel the reaction).</summary>
    private const float VISIBLE_GRACE = 1f;

    /// <summary>Within this fraction of the bot's reach a visible shooter counts as seen immediately.</summary>
    private const float CLOSE_SIGHT_REACH_FRACTION = 0.4f;

    /// <summary>Once the bot has seen the shooter it keeps counting as seen for this long (a flicker of the vision flag must not restart the reaction).</summary>
    private const float SEEN_HOLD = 4f;

    /// <summary>Within this share of the reach (a strong weapon at a short distance) Cover is capped, the excess goes to fighting back.</summary>
    private static float STRONG_WEAPON_RATIO => GlobalSettings.Fork.ShotStrongWeaponRatio;
    private static float STRONG_WEAPON_COVER_MAX => GlobalSettings.Fork.ShotStrongWeaponCoverMax;

    /// <summary>Investigate stops advancing when the bot is this close to the vague shot area (it is in ambush range) or hears the shooter.</summary>
    private static float INVESTIGATE_STOP_DISTANCE => GlobalSettings.Fork.ShotInvestigateStopDistance;
    private const float INVESTIGATE_SOUND_AGE = 1.5f;
    private const float INVESTIGATE_SOUND_GRACE = 2f;

    /// <summary>After stopping, the bot holds cover and watches this long (scaled by personality), then the state ends.</summary>
    private static float AMBUSH_TIME_MIN => GlobalSettings.Fork.ShotAmbushMin;

    /// <summary>In an advance the bot holds each cover this long (scaled by personality) before it runs to the next one.</summary>
    private static float HOP_HOLD_MIN => GlobalSettings.Fork.ShotHopHoldMin;

    /// <summary>After a retreat arrives the bot holds cover this long (scaled by personality), then investigates cover to cover instead of strolling back.</summary>
    private static float POST_RETREAT_HOLD_MIN => GlobalSettings.Fork.ShotPostRetreatHoldMin;
    private static float POST_RETREAT_HOLD_MAX => GlobalSettings.Fork.ShotPostRetreatHoldMax;

    /// <summary>A hop that has not reached its cover after this long, or has stalled (not moving) for HOP_STALL_TIME, is abandoned; no new hop for HOP_RETRY_DELAY.</summary>
    private const float HOP_STALL_TIME = 3f;
    private const float HOP_RETRY_DELAY = 5f;

    /// <summary>A hit while hiding keeps the bot hiding at least this much longer.</summary>
    private const float HIDE_EXTEND = 2f;

    /// <summary>After an investigation was broken by fire, the next break is only allowed after this long (so a player cannot kite a bot forever with single shots).</summary>
    private static float BREAK_LOCKOUT => GlobalSettings.Fork.ShotBreakLockout;

    /// <summary>A state whose hook never ran (the bot was busy with something earlier in the decision order) is ended this long after it expired.</summary>
    private const float EXPIRY_GRACE = 1f;

    /// <summary>Gunshots from the same shooter within this time reuse the last vague position (every bot in earshot computes one).</summary>
    private const float DEGRADE_THROTTLE = 0.75f;

    /// <summary>Self preservation: a hit (or a close bullet) while the bot shelters in cover makes it look for another cover. Chances, scaled by personality; at most one move per interval.</summary>
    private static float SELF_PRESERVE_HIT_CHANCE => GlobalSettings.Fork.ShotSelfPreserveHit;
    private static float SELF_PRESERVE_BULLET_CHANCE => GlobalSettings.Fork.ShotSelfPreserveBullet;
    private static float SELF_PRESERVE_INTERVAL => GlobalSettings.Fork.ShotSelfPreserveInterval;

    /// <summary>
    /// A shooter this close (a quarter of the bot's reach, but at least 10 m and at most 25 m) is fought, not fled from:
    /// no self-preservation hop to another cover, and a hiding bot may fire back at the vague area.
    /// </summary>
    private static float CLOSE_RANGE_MIN => GlobalSettings.Fork.ShotCloseRangeMin;
    private static float CLOSE_RANGE_MAX => GlobalSettings.Fork.ShotCloseRangeMax;
    private static float CLOSE_RANGE_REACH_FRACTION => GlobalSettings.Fork.ShotCloseRangeReachFraction;

    /// <summary>
    /// Point blank (hard stop): shot from this close, even by an unseen shooter, running away does not work. Concealment (suppressor, prone) no longer
    /// matters and Fight gets at least this share, whatever the weapon, personality or state.
    /// </summary>
    private static float POINT_BLANK_DISTANCE => GlobalSettings.Fork.ShotPointBlankDistance;
    private static float POINT_BLANK_FIGHT_SHARE => GlobalSettings.Fork.ShotPointBlankFightShare;

    /// <summary>A hit (or a close bullet) while investigating breaks the advance: the bot runs for cover and reassesses (a new Cover reaction, which ends in a new follow-up pick).</summary>
    private static float INVESTIGATE_BREAK_HIT_CHANCE => GlobalSettings.Fork.ShotInvestigateBreakHit;
    private static float INVESTIGATE_BREAK_BULLET_CHANCE => GlobalSettings.Fork.ShotInvestigateBreakBullet;
    private static float HOP_HOLD_MAX => GlobalSettings.Fork.ShotHopHoldMax;
    private static float AMBUSH_TIME_MAX => GlobalSettings.Fork.ShotAmbushMax;

    /// <summary>No usable cover: the finder may still be scanning, so the decision waits this long (the bot runs its short stopgap meanwhile).</summary>
    private static float NO_COVER_GRACE => GlobalSettings.Fork.ShotNoCoverGrace;

    private enum EFollowUp
    {
        Retreat,
        FightOn,
        Investigate,
    }

    public EShotReaction Outcome { get; private set; }

    /// <summary>True while the bot investigates and moves: SAIN's search then sprints (when it is not waiting or looking at the enemy).</summary>
    public bool SprintWhileInvestigating { get; private set; }

    /// <summary>True while the bot walks away from the area it was shot at (after hiding).</summary>
    public bool Relocating { get; private set; }

    /// <summary>Where the bot is walking to while relocating.</summary>
    public Vector3 RelocatePoint { get; private set; }

    public ShotReactionClass(BotComponent bot)
        : base(bot)
    {
        // Everything is event driven or evaluated when asked; ManualUpdate only runs the diagnostic trace (early return when idle).
        CanEverTick = true;
    }

    private bool _deathHooked;
    private bool _enemyHitHooked;

    private void HookEnemyHit()
    {
        if (!_enemyHitHooked && Bot.EnemyController?.Events != null)
        {
            Bot.EnemyController.Events.OnEnemyHit += OnBotHitEnemy;
            _enemyHitHooked = true;
        }
    }

    /// <summary>This bot hit one of its enemies: for the player, a duel retreat needs the bot to have failed to hit it.</summary>
    private void OnBotHitEnemy(Enemy enemy)
    {
        if (enemy?.EnemyPlayer?.IsYourPlayer == true)
        {
            _duelLastHitPlayer = Time.time;
            // The bot is hitting its target: no duel retreat for now.
            _duelSeen = 0f;
            _duelBursts.Clear();
        }
    }

    private void HookDeath()
    {
        if (!_deathHooked && Bot.Player != null)
        {
            Bot.Player.OnPlayerDead += OnOwnDeath;
            _deathHooked = true;
        }
    }

    public override void Init()
    {
        base.Init();
        HookDeath();
    }

    public override void Dispose()
    {
        if (_deathHooked && Bot.Player != null)
        {
            Bot.Player.OnPlayerDead -= OnOwnDeath;
            _deathHooked = false;
        }
        if (_enemyHitHooked && Bot.EnemyController?.Events != null)
        {
            Bot.EnemyController.Events.OnEnemyHit -= OnBotHitEnemy;
            _enemyHitHooked = false;
        }
        base.Dispose();
    }

    /// <summary>True if the fork's shot reaction is switched on for this bot (master switch and brain filter).</summary>
    public bool Enabled
    {
        get { return GlobalSettings.Fork.ShotReactionEnabled && Bot.Info.Profile.ShotReactionEnabled; }
    }

    /// <summary>
    /// True while a shot reaction is waiting for its first cover (no follow-up pick yet, no fight-or-flight decision yet, not retreating):
    /// the cover search then treats "only foliage" as "no cover".
    /// </summary>
    public bool NoCoverDecisionPending
    {
        get { return Outcome != EShotReaction.None && Outcome != EShotReaction.Fight && !Relocating && !_followDone && !_noCoverDone; }
    }

    /// <summary>The bot is in a shot reaction (hiding, holding, investigating, retreating or fighting).</summary>
    public bool InReaction
    {
        get { return Outcome != EShotReaction.None || Relocating; }
    }

    public bool IsManaging(Enemy enemy)
    {
        return Outcome != EShotReaction.None && enemy != null && enemy == _enemy;
    }

    /// <summary>The reaction owns this bot's decision (hide, hold, investigate, retreat, fight): squad decisions such as following the leader wait until it is over.</summary>
    public bool OverridesSquad(Enemy enemy)
    {
        return Enabled && enemy != null && enemy == _enemy && (Relocating || Outcome != EShotReaction.None);
    }

    /// <summary>
    /// The reaction applies to shots from this enemy: switched on, the shooter is you (if configured), and the bot cannot see it.
    /// </summary>
    public bool AppliesTo(Enemy enemy)
    {
        if (enemy == null || !Enabled)
        {
            return false;
        }
        if (GlobalSettings.Fork.ShotReactionPlayerOnly && enemy.EnemyPlayer?.IsYourPlayer != true)
        {
            return false;
        }
        return !SeesShooter(enemy);
    }

    /// <summary>The bot has really seen the shooter: continuously visible for the grace time.</summary>
    private bool SeesShooter(Enemy enemy)
    {
        if (enemy == null)
        {
            return false;
        }
        float now = Time.time;
        if (SeesShooterNow(enemy))
        {
            _lastSeenEnemy = enemy;
            _lastSeenTime = now;
            return true;
        }
        return enemy == _lastSeenEnemy && now - _lastSeenTime < SEEN_HOLD;
    }

    private bool SeesShooterNow(Enemy enemy)
    {
        if (!enemy.IsVisible)
        {
            return false;
        }
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        float distance = enemy.RealDistance;
        // Up close a visible enemy is seen at once: the bot fights normally, no grace time.
        if (distance <= reach * CLOSE_SIGHT_REACH_FRACTION)
        {
            return true;
        }
        if (enemy.VisibleDuration < VISIBLE_GRACE)
        {
            return false;
        }
        // A shooter beyond the bot's weapon reach cannot be located by a shot alone: the vision flag does not make it "seen".
        return distance <= reach * FIGHT_REACH_LIMIT;
    }

    /// <summary>
    /// While a reaction is active and the bot has not really seen the shooter, it looks where it moves (or at the vague area when still), never at the exact position.
    /// True = steering handled.
    /// </summary>
    public bool TrySteer(Enemy enemy)
    {
        if (!Relocating && (!IsManaging(enemy) || Outcome == EShotReaction.Fight))
        {
            return false;
        }
        // The bot sees the shooter, or is aiming at something it can see: normal steering looks where it aims.
        if (SeesShooter(enemy) || Bot.ManualShoot.Reason != EShootReason.None || Bot.Aim.AimStatus != AimStatus.NoTarget)
        {
            return false;
        }
        string target = null;
        if (Bot.Mover.Moving && Bot.Steering.LookToMovingDirection())
        {
            target = "movement direction";
        }
        else if (Time.time - _vaguePositionTime < VAGUE_POSITION_MAX_AGE)
        {
            Bot.Steering.LookToFloorPoint(_vaguePosition);
            target = "vague shot area";
        }
        if (target != null && !_steerLogged)
        {
            _steerLogged = true;
            if (GlobalSettings.Fork.ShotReactionLog)
            {
                Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Tag}: steering to the {target}, not the shooter");
            }
        }
        return target != null;
    }

    /// <summary>
    /// Replaces a position learned from a shot with a vague one: a random point around the shooter, further off the further away it is.
    /// </summary>
    public Vector3 DegradePosition(Enemy enemy, Vector3 estimated, float distance, bool hit)
    {
        if (!AppliesTo(enemy))
        {
            return estimated;
        }
        ForkSettings fork = GlobalSettings.Fork;
        float now = Time.time;
        // Every bot in earshot of a gunshot gets here: a bullet that is not a hit reuses the last vague position for a moment (no navmesh query).
        if (!hit && _vaguePositionTime > 0f && now - _lastDegradeTime < DEGRADE_THROTTLE)
        {
            return _vaguePosition;
        }
        _lastDegradeTime = now;

        float uncertainty = Shrunk(ComputeUncertainty(enemy.EnemyPlayer, distance, hit, enemy), NoteInformation(now, PieceWeight(hit)));
        float radius = Mathf.Max(Mathf.Min(uncertainty, ErrorCeiling(fork)), fork.ShotPositionErrorMin);
        Vector2 offset = RingOffset(radius * fork.ShotAreaInnerFraction, radius);
        Vector3 point = SnapKeepingRing(enemy.EnemyPosition + new Vector3(offset.x, 0f, offset.y), enemy.EnemyPosition, radius * fork.ShotAreaInnerFraction, radius);
        // Never the shooter's exact position: if there is no navmesh near the offset point, the raw offset point is the vague area.
        RememberVaguePosition(point, uncertainty);
        LogEvent($"position error {radius:0} m at {distance:0} m{(hit ? " (hit)" : "")}: {ErrorBreakdown()}");
        return point;
    }

    /// <summary>
    /// Concealment widens the position error: a prone or crouched shooter, a bush and stealthy gear are harder to place (factors multiply, 1 = no concealment). It widens the error before the cap.
    /// The suppressor is not here: it shortens the weapon's audible range, see AudibilityErrorFactor.
    /// </summary>
    public static float ConcealmentErrorFactor(IPlayer shooter, float distance)
    {
        if (shooter == null)
        {
            return 1f;
        }
        ForkSettings fork = GlobalSettings.Fork;
        float factor = 1f;
        Player player = shooter as Player;
        if (player?.IsInPronePose == true)
        {
            factor *= fork.ShotProneErrorFactor;
        }
        else if (player?.Pose == EPlayerPose.Duck)
        {
            factor *= fork.ShotCrouchErrorFactor;
        }
        if (fork.ShotBushErrorFactor != 1f && InBush(shooter))
        {
            factor *= fork.ShotBushErrorFactor;
        }
        if (fork.ShotGearErrorWeight > 0f)
        {
            float gear = GameWorldComponent.Instance?.PlayerTracker?.GetPlayerComponent(shooter.ProfileId)?.AIData?.AIGearModifier?.StealthModifier(distance) ?? 1f;
            factor *= Mathf.Lerp(1f, Mathf.Clamp(gear, 0.7f, 1.6f), fork.ShotGearErrorWeight);
        }
        return factor;
    }

    private struct TracerState
    {
        public float Steps;
        public float Last;
        public float LastCount;
    }

    private const float TRACER_COUNT_INTERVAL = 0.08f;
    private const float TRACER_MAX_STEPS = 30f;
    private static readonly Dictionary<string, TracerState> _tracers = [];
    private static readonly Collider[] _bushColliders = new Collider[4];
    private static int _bushMask = -1;

    /// <summary>The tracer steps of a shooter now: the stored steps minus the slow decay since the last noticed tracer (0 after the max memory).</summary>
    private static float TracerSteps(TracerState state, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (state.Steps <= 0f || now - state.Last > fork.ShotTracerMaxMemory)
        {
            return 0f;
        }
        return Mathf.Max(0f, state.Steps - (now - state.Last) / Mathf.Max(fork.ShotTracerDecayTime, 1f));
    }

    /// <summary>
    /// A tracer round of the shooter flew past or landed near a bot: one more step on the shooter's tracer ramp (at most once per TRACER_COUNT_INTERVAL, since several bots note the same rounds).
    /// The steps decay slowly (Tracer Decay Time) and are forgotten after Tracer Max Memory; a bot only uses them while the shooter is inside its rough area (see TracerRamp).
    /// </summary>
    public static void NoteTracer(Shot bullet)
    {
        IPlayer shooter = bullet?.Player?.iPlayer;
        if (shooter != null && bullet.Ammo is Ammo ammo && ammo.AmmoTemplate != null && ammo.AmmoTemplate.Tracer)
        {
            float now = Time.time;
            _tracers.TryGetValue(shooter.ProfileId, out TracerState state);
            state.Steps = TracerSteps(state, now);
            if (state.Steps <= 0f)
            {
                state.Steps = 1f;
                state.LastCount = now;
            }
            else if (now - state.LastCount >= TRACER_COUNT_INTERVAL)
            {
                state.Steps = Mathf.Min(state.Steps + 1f, TRACER_MAX_STEPS);
                state.LastCount = now;
            }
            state.Last = now;
            _tracers[shooter.ProfileId] = state;
        }
    }

    /// <summary>
    /// True while the shooter is inside the rough area this bot holds (it has not moved farther than the area's radius times Tracer Ring Margin from where the bot last placed it).
    /// A bot without a fresh area counts as inside.
    /// </summary>
    private bool ShooterInOwnArea(IPlayer shooter)
    {
        if (_vaguePositionTime <= 0f || Time.time - _vaguePositionTime > VAGUE_POSITION_MAX_AGE || shooter == null)
        {
            return true;
        }
        Vector3 delta = shooter.Position - _vagueShooterPosition;
        delta.y = 0f;
        float radius = Mathf.Max(_vagueUncertainty, GlobalSettings.Fork.ShotPositionErrorMin) * GlobalSettings.Fork.ShotTracerRingMargin;
        return delta.magnitude <= radius;
    }

    /// <summary>The tracer ramp of this shooter for this bot: the error factor (1 = no ramp) and how far the ramp has come (0 to 1, for the suppressor cancel and the narrowing).</summary>
    private float TracerRamp(IPlayer shooter, out float progress)
    {
        progress = 0f;
        ForkSettings fork = GlobalSettings.Fork;
        if (shooter == null || !_tracers.TryGetValue(shooter.ProfileId, out TracerState state))
        {
            return 1f;
        }
        float steps = TracerSteps(state, Time.time);
        if (steps <= 0f || !ShooterInOwnArea(shooter))
        {
            return 1f;
        }
        float shots = Mathf.Max(fork.ShotTracerRampShots, 2f);
        progress = Mathf.Clamp01(steps / shots);
        float t = Mathf.Clamp01((steps - 1f) / (shots - 1f));
        return Mathf.Lerp(fork.ShotTracerFirstFactor, fork.ShotTracerMinFactor, t);
    }

    /// <summary>
    /// How well the shot can be placed by sound: distance divided by the range the shooter's weapon is heard at (caliber, muzzle devices and a suppressor already in it).
    /// Flat up to Audible Start, then a smooth rise to the peak factor at the peak ratio, then a slow rise to the max factor one audible range further out.
    /// Sustained tracer fire moves the range back toward the unsuppressed one.
    /// </summary>
    private float AudibilityErrorFactor(IPlayer shooter, float distance)
    {
        _dbgAudRange = 0f;
        _dbgAudRatio = 0f;
        ForkSettings fork = GlobalSettings.Fork;
        var info = GameWorldComponent.Instance?.PlayerTracker?.GetPlayerComponent(shooter?.ProfileId)?.Equipment?.CurrentWeaponInfo;
        if (info == null)
        {
            return 1f;
        }
        float range = info.CalculatedAudibleRange;
        if (info.HasSuppressor && fork.ShotTracerSuppressorCancel > 0f)
        {
            TracerRamp(shooter, out float progress);
            if (progress > 0f)
            {
                float loud = info.BaseAudibleRange + (info.FlashHider?.Loudness ?? 0f);
                range = Mathf.Lerp(range, Mathf.Max(loud, range), fork.ShotTracerSuppressorCancel * progress);
            }
        }
        range = Mathf.Max(range, 10f);
        float r = distance / range;
        _dbgAudRange = range;
        _dbgAudRatio = r;

        float start = fork.ShotAudibleStart;
        float peakRatio = Mathf.Max(fork.ShotAudiblePeakRatio, start + 0.05f);
        float peak = Mathf.Max(fork.ShotAudiblePeakFactor, 1f);
        if (r <= start)
        {
            return 1f;
        }
        if (r <= peakRatio)
        {
            float t = (r - start) / (peakRatio - start);
            return 1f + (peak - 1f) * t * t;
        }
        float max = Mathf.Max(fork.ShotAudibleMaxFactor, peak);
        return Mathf.Min(max, peak + (max - peak) * (r - peakRatio));
    }

    /// <summary>
    /// Inside to outside: a shooter in a building (environment id above 0) and a bot outside cannot tell which building a shot came from, more so the further away it is.
    /// Outside to inside gets a milder version at long range. Building to building and outside to outside are unchanged (1).
    /// </summary>
    private float EnvironmentErrorFactor(IPlayer shooter, float distance)
    {
        int shooterEnv = (shooter as Player)?.AIData?.EnvironmentId ?? 0;
        int botEnv = Bot.Player?.AIData?.EnvironmentId ?? 0;
        _dbgShooterEnv = shooterEnv;
        _dbgBotEnv = botEnv;
        ForkSettings fork = GlobalSettings.Fork;
        if (shooterEnv > 0 && botEnv == 0)
        {
            return RampFactor(distance, fork.ShotEnvInOutStart, fork.ShotEnvInOutFull, fork.ShotEnvInOutFactor);
        }
        if (shooterEnv == 0 && botEnv > 0)
        {
            return RampFactor(distance, fork.ShotEnvOutInStart, fork.ShotEnvOutInFull, fork.ShotEnvOutInFactor);
        }
        return 1f;
    }

    /// <summary>1 up to start, a smooth rise to the factor at full, the factor beyond.</summary>
    private static float RampFactor(float distance, float start, float full, float factor)
    {
        if (factor <= 1f || distance <= start)
        {
            return 1f;
        }
        float t = Mathf.Clamp01((distance - start) / Mathf.Max(full - start, 1f));
        t = t * t * (3f - 2f * t);
        return 1f + (factor - 1f) * t;
    }

    /// <summary>Up to Position Error Max the value is unchanged; above it only creeps toward Position Error Soft Max (exponential approach, Soft Length meters of raw error for 63%).</summary>
    public static float SoftCap(float value)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float knee = fork.ShotPositionErrorMax;
        float room = fork.ShotPositionErrorSoftMax - knee;
        if (value <= knee)
        {
            return value;
        }
        if (room <= 0f)
        {
            return knee;
        }
        return knee + room * (1f - Mathf.Exp(-(value - knee) / Mathf.Max(fork.ShotPositionErrorSoftLength, 1f)));
    }

    /// <summary>The largest radius a ring or an uncertainty can have (the soft maximum, or the knee if that is lower).</summary>
    private static float ErrorCeiling(ForkSettings fork)
    {
        return Mathf.Max(fork.ShotPositionErrorMax, fork.ShotPositionErrorSoftMax);
    }

    private float _dbgBase;
    private float _dbgAudRange;
    private float _dbgAudRatio;
    private float _dbgAud;
    private float _dbgEnv;
    private float _dbgConc;
    private float _dbgTracer;
    private float _dbgTracerProgress;
    private int _dbgShooterEnv;
    private int _dbgBotEnv;

    /// <summary>The shooter stands in a bush: foliage colliders around its chest.</summary>
    private static bool InBush(IPlayer shooter)
    {
        if (_bushMask < 0)
        {
            _bushMask = 1 << LayerMask.NameToLayer("Foliage");
        }
        return Physics.OverlapSphereNonAlloc(shooter.Position + Vector3.up * 0.6f, 0.8f, _bushColliders, _bushMask) > 0;
    }

    /// <summary>
    /// The error of a SIGHTING (a bot that sees the player reports it): only what a pair of eyes has. The distance over the bot's sight range (as for a shot), times concealment
    /// (prone, crouch, bush, gear), soft capped. No audibility, environment or tracer: those belong to sound.
    /// </summary>
    public float ComputeVisualUncertainty(IPlayer player, float distance, Enemy enemy)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float ratio = distance / SightDistance(enemy);
        float sightBased = ratio <= 1f
            ? fork.ShotUncertaintyAtSight * ratio
            : fork.ShotUncertaintyAtSight + fork.ShotUncertaintyBeyondSight * (ratio - 1f);
        _dbgBase = sightBased;
        _dbgAudRange = 0f;
        _dbgAudRatio = 0f;
        _dbgAud = 1f;
        _dbgEnv = 1f;
        _dbgTracer = 1f;
        _dbgConc = ConcealmentErrorFactor(player, distance);
        return SoftCap(sightBased * _dbgConc);
    }

    /// <summary>
    /// What is lost when information travels from its source to a listener (meters): a small error for everyone, then a slow growth after the free range and a faster one after the near end.
    /// The source itself loses nothing (the caller passes 0).
    /// </summary>
    private static float TransmissionError(float distance)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float error = fork.ShareBaseError;
        float nearEnd = Mathf.Max(fork.ShareNearEnd, fork.ShareFreeRange);
        float near = Mathf.Min(distance, nearEnd) - fork.ShareFreeRange;
        if (near > 0f)
        {
            error += near * fork.ShareNearPerMeter;
        }
        float far = distance - nearEnd;
        if (far > 0f)
        {
            error += far * fork.ShareFarPerMeter;
        }
        return error;
    }

    private float _sharedKey = -1f;
    private Vector2 _sharedJitter;
    private bool _vagueShared;
    private bool _vagueSighting;

    /// <summary>
    /// Takes information from another bot: the source's area (point and radius) as it came, blurred by the distance to the source (the radius grows by the transmission error, soft capped,
    /// and the point moves by a random offset of at most that growth, so the player stays inside). The offset is rolled once per piece of shared information (key), not on every repeat.
    /// A fresh, tighter area this bot made from its own senses is kept. Returns the position the bot holds afterwards.
    /// </summary>
    private Vector3 TakeShared(Vector3 point, float sourceUncertainty, float transmission, float key, string what, string from, float distance, bool sighting = false)
    {
        float now = Time.time;
        float uncertainty = SoftCap(sourceUncertainty + transmission);
        float grown = Mathf.Max(0f, uncertainty - sourceUncertainty);
        float jitter = Mathf.Min(transmission, grown);
        if (jitter > 0.01f)
        {
            if (key != _sharedKey || key < 0f)
            {
                _sharedKey = key;
                _sharedJitter = Random.insideUnitCircle;
            }
            Vector3 raw = point + new Vector3(_sharedJitter.x, 0f, _sharedJitter.y) * jitter;
            point = SnapKeepingRing(raw, point, 0f, jitter);
        }
        if (!_vagueShared && _vaguePositionTime > 0f && now - _vaguePositionTime <= GlobalSettings.Fork.AreaShrinkCooldown && _vagueUncertainty <= uncertainty)
        {
            return _vaguePosition;
        }
        RememberVaguePosition(point, uncertainty);
        _vagueShared = true;
        _vagueSighting = sighting;
        if (key < 0f || key != _lastShareLogKey)
        {
            _lastShareLogKey = key;
            LogEvent($"shared {what} from {from}: source error {sourceUncertainty:0} m, blur +{transmission:0.0} m at {distance:0} m from the source, area {uncertainty:0} m");
        }
        return point;
    }

    private float _lastShareLogKey = -2f;

    /// <summary>
    /// How unsure this bot is where a shot came from (meters). The base is the larger of the sight-based error (20 m at exactly the bot's view distance, linear closer, 15 m more per further
    /// view distance) and distance times Error Per Meter. On top: a hit (small bonus), the audibility of the shot (its range from caliber, muzzle devices and suppressor), the environment
    /// (inside to outside grows with the distance), concealment (prone, crouched, bush, gear) and the tracer ramp. The result goes through the soft cap (knee Position Error Max, creeping to Soft Max).
    /// </summary>
    public float ComputeUncertainty(IPlayer shooter, float distance, bool hit, Enemy enemy)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float ratio = distance / SightDistance(enemy);
        float sightBased = ratio <= 1f
            ? fork.ShotUncertaintyAtSight * ratio
            : fork.ShotUncertaintyAtSight + fork.ShotUncertaintyBeyondSight * (ratio - 1f);
        float baseUncertainty = Mathf.Max(sightBased, distance * fork.ShotErrorPerMeter);
        _dbgBase = baseUncertainty;
        _dbgAud = AudibilityErrorFactor(shooter, distance);
        _dbgEnv = EnvironmentErrorFactor(shooter, distance);
        _dbgConc = ConcealmentErrorFactor(shooter, distance);
        _dbgTracer = TracerRamp(shooter, out _dbgTracerProgress);
        _lastShooterPosition = shooter != null ? shooter.Position : _lastShooterPosition;
        float uncertainty = baseUncertainty * (hit ? fork.ShotHitErrorFactor : 1f) * _dbgAud * _dbgEnv * _dbgConc * _dbgTracer;
        return SoftCap(uncertainty);
    }

    /// <summary>One log line with the parts of the last position error this bot computed (see ComputeUncertainty).</summary>
    private string ErrorBreakdown()
    {
        return $"base {_dbgBase:0.0} m, audibility x{_dbgAud:0.00} (heard to {_dbgAudRange:0} m, at {_dbgAudRatio:0.00} of it), environment x{_dbgEnv:0.00} (shooter env {_dbgShooterEnv}, bot env {_dbgBotEnv}), concealment x{_dbgConc:0.00}, tracer x{_dbgTracer:0.00}";
    }

    /// <summary>The bot's current effective view distance for this shooter (meters): its visible distance plus the per shooter bonus (movement, flare, gear), at least the minimum sight.</summary>
    private float SightDistance(Enemy enemy)
    {
        float sight = Bot.BotOwner?.LookSensor != null ? Bot.BotOwner.LookSensor.VisibleDist : 0f;
        if (enemy?.Vision?._visionDistance != null)
        {
            sight += enemy.Vision._visionDistance.Value;
        }
        return Mathf.Max(sight, GlobalSettings.Fork.ShotUncertaintyMinSight);
    }

    /// <summary>
    /// The rough shot area while the bot is in a shot reaction and it is fresh: the cover finder uses it as the threat position when the bot has no known position of a target
    /// (an alerted bot has an enemy record but nothing in its known places). Null otherwise.
    /// </summary>
    public Vector3? ThreatArea
    {
        get
        {
            if (!Enabled || !GlobalSettings.Fork.ShotCoverFromArea || _vaguePositionTime <= 0f || Time.time - _vaguePositionTime > VAGUE_POSITION_MAX_AGE)
            {
                return null;
            }
            return Outcome != EShotReaction.None || Relocating ? _vaguePosition : null;
        }
    }

    /// <summary>True while the bot is in a shot reaction (hiding, retreating, investigating): its cover search is wider and taller (see CoverFinderComponent).</summary>
    public bool WideCoverSearch
    {
        get { return Enabled && GlobalSettings.Fork.ShotCoverWideSearch && (Outcome != EShotReaction.None || Relocating); }
    }

    /// <summary>The uncertainty of the rough shot area while it is fresh (0 when there is no fresh area).</summary>
    public float FreshUncertainty
    {
        get { return Time.time - _vaguePositionTime <= VAGUE_POSITION_MAX_AGE ? _vagueUncertainty : 0f; }
    }

    /// <summary>
    /// A random offset in the ring between inner and outer (uniform over the ring's area): the rough area is never closer to the shooter than inner,
    /// so a lucky draw cannot put a searching bot on top of the player. inner 0 = the whole disk.
    /// </summary>
    private static Vector2 RingOffset(float inner, float outer)
    {
        inner = Mathf.Clamp(inner, 0f, outer);
        float radius = Mathf.Sqrt(Random.Range(inner * inner, outer * outer));
        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    /// <summary>
    /// One piece of information about the player's position reached this bot (a shot, hit, alert or report). Counts at most once per cooldown (a burst is one piece)
    /// and the count is forgotten after the memory time without any. Returns the shrink factor for areas made now (1 for the first piece).
    /// </summary>
    private sealed class KnowState
    {
        public int Count;
        /// <summary>Weighted pieces beyond the first: the exponent of the shrink (the first piece never narrows).</summary>
        public float Extra;
        public float LastCount = -999f;
        public float LastInfo = -999f;
    }

    private static readonly Dictionary<BotsGroup, KnowState> _groupKnow = [];
    private readonly KnowState _ownKnow = new();

    /// <summary>The count of information pieces this bot narrows its areas by: its squad's (everyone in a squad has the same ring) or its own when it has no group or sharing is off.</summary>
    private KnowState GetKnowState()
    {
        if (GlobalSettings.Fork.AreaShareSquad)
        {
            BotsGroup group = Bot.BotOwner?.BotsGroup;
            if (group != null)
            {
                if (!_groupKnow.TryGetValue(group, out KnowState state))
                {
                    if (_groupKnow.Count > 64)
                    {
                        List<BotsGroup> stale = null;
                        foreach (var pair in _groupKnow)
                        {
                            if (Time.time - pair.Value.LastInfo > 300f)
                            {
                                (stale ??= []).Add(pair.Key);
                            }
                        }
                        if (stale != null)
                        {
                            foreach (BotsGroup key in stale)
                            {
                                _groupKnow.Remove(key);
                            }
                        }
                    }
                    state = new KnowState();
                    _groupKnow[group] = state;
                }
                return state;
            }
        }
        return _ownKnow;
    }

    private int KnowCount
    {
        get { return GetKnowState().Count; }
    }

    /// <summary>
    /// One piece of information about the player's position reached this bot (a shot, hit, alert or report). Counts at most once per cooldown (a burst is one piece)
    /// and the count is forgotten after the memory time without any. The count belongs to the bot's squad (every member narrows at once); a Rogue or Goon also hands it to
    /// the Rogues and Goons close to it. Returns the shrink factor for areas made now (1 for the first piece).
    /// </summary>
    private float NoteInformation(float now, float weight = 1f)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float shrink = Mathf.Clamp(fork.AreaShrink, 0.1f, 1f);
        if (shrink >= 1f)
        {
            return 1f;
        }
        KnowState state = GetKnowState();
        if (now - state.LastInfo > fork.AreaMemory)
        {
            state.Count = 0;
            state.Extra = 0f;
        }
        state.LastInfo = now;
        // Sustained tracer fire cuts the cooldown, so a burst of tracers counts as several pieces.
        float cooldown = fork.AreaShrinkCooldown * (1f - Mathf.Clamp01(fork.AreaTracerCooldownCut) * _dbgTracerProgress);
        if (state.Count == 0 || now - state.LastCount >= cooldown)
        {
            state.Count++;
            if (state.Count >= 2)
            {
                state.Extra += weight;
            }
            state.LastCount = now;
            ShareWithNeighbors(state, now);
            if (state.Count >= 2 && (state.Count <= 3 || state.Count % 5 == 0))
            {
                LogEvent($"more information about the player's position: piece #{state.Count} (weight {weight:0.00}), areas now x{Mathf.Pow(shrink, state.Extra):0.00} (floor {FloorFraction() * 100f:0}% of the error)");
            }
        }
        return Mathf.Pow(shrink, state.Extra);
    }

    /// <summary>
    /// How much one piece of information counts: a shot barely audible to this bot counts less (hits always in full), sustained tracer fire counts more,
    /// and a concealed shooter slows it down. Reads the parts of the last position error this bot computed (call after ComputeUncertainty).
    /// </summary>
    private float PieceWeight(bool hit)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float weight = 1f;
        if (!hit && _dbgAudRange > 0f)
        {
            float start = fork.ShotAudibleStart;
            float peak = Mathf.Max(fork.ShotAudiblePeakRatio, start + 0.05f);
            float t = Mathf.Clamp01((_dbgAudRatio - start) / (peak - start));
            weight = Mathf.Lerp(1f, fork.AreaFarPieceWeight, t);
        }
        weight *= 1f + fork.AreaTracerPieceBonus * _dbgTracerProgress;
        weight /= Mathf.Lerp(1f, Mathf.Max(_dbgConc, 1f), fork.AreaConcealmentWeight);
        return weight;
    }

    /// <summary>The share of its starting error a rough area can narrow down to (lower while tracers are noticed).</summary>
    private float FloorFraction()
    {
        ForkSettings fork = GlobalSettings.Fork;
        return Mathf.Lerp(fork.AreaFloorFraction, fork.AreaFloorFractionTracer, _dbgTracerProgress);
    }

    /// <summary>A Rogue or Goon that narrowed hands the new count to the Rogues and Goons within AreaShareRadius of it (they get the same ring at once).</summary>
    private void ShareWithNeighbors(KnowState state, float now)
    {
        float radius = GlobalSettings.Fork.AreaShareRadius;
        if (radius <= 0f || state.Count < 2 || !IsRogueOrGoon(Bot.BotOwner))
        {
            return;
        }
        BotManagerComponent manager = BotManagerComponent.Instance;
        if (manager?.Bots == null)
        {
            return;
        }
        float radiusSqr = radius * radius;
        foreach (BotComponent other in manager.Bots.Values)
        {
            if (other == null || other == Bot || other.BotOwner == null || other.BotOwner.IsDead || other.ShotReaction == null || !IsRogueOrGoon(other.BotOwner))
            {
                continue;
            }
            if ((other.Position - Bot.Position).sqrMagnitude > radiusSqr)
            {
                continue;
            }
            KnowState neighbor = other.ShotReaction.GetKnowState();
            // Compare the weighted total (tracer pieces weigh more than their count), not the raw count.
            if (neighbor != state && neighbor.Extra < state.Extra)
            {
                neighbor.Count = Mathf.Max(neighbor.Count, state.Count);
                neighbor.Extra = state.Extra;
                neighbor.LastCount = now;
                neighbor.LastInfo = now;
            }
        }
    }

    /// <summary>The value narrowed by the factor, but never below the floor share of itself (an area that was smaller to begin with is not widened).</summary>
    private float Shrunk(float value, float factor)
    {
        return factor >= 1f ? value : value * Mathf.Max(factor, FloorFraction());
    }

    /// <summary>
    /// Puts a rough point on the navmesh, but only if that keeps it inside its ring (not closer to the shooter than inner, not farther than outer):
    /// snapping to the nearest navmesh point can pull the point right next to the player, which would undo the minimum distance.
    /// Without a navmesh point near, or with a snapped point outside the ring, the raw point is used.
    /// </summary>
    private static Vector3 SnapKeepingRing(Vector3 raw, Vector3 center, float inner, float outer)
    {
        if (NavMesh.SamplePosition(raw, out NavMeshHit hit, Mathf.Max(5f, outer * 0.5f), NavMesh.AllAreas))
        {
            float away = new Vector2(hit.position.x - center.x, hit.position.z - center.z).magnitude;
            if (away >= inner * 0.95f && away <= outer * 1.05f)
            {
                return hit.position;
            }
        }
        return raw;
    }

    private void RememberVaguePosition(Vector3 position, float uncertainty)
    {
        _vagueShared = false;
        _vagueSighting = false;
        _vaguePosition = position;
        _vagueShooterPosition = _lastShooterPosition;
        _vaguePositionTime = Time.time;
        _vagueUncertainty = uncertainty;
    }

    /// <summary>
    /// The bot was hit by an enemy it cannot see: it learns a vague area, then rolls a reaction.
    /// </summary>
    public void OnHit(Enemy enemy)
    {
        NoteDuelHit(enemy, true);
        if (!AppliesTo(enemy))
        {
            LogSkipped(enemy, true);
            return;
        }
        float distance = enemy.RealDistance;
        Vector3 position = DegradePosition(enemy, enemy.EnemyPosition, distance, true);
        SAINHearingReport report = new()
        {
            position = position,
            soundType = SAINSoundType.Shot,
            placeType = EEnemyPlaceType.Hearing,
            isDanger = true,
            shallReportToSquad = true,
        };
        enemy.Hearing.SetHeard(report, Time.time);
        // Being shot is not "heard from peace": SAIN would freeze (indoors) or search stealthily because of it.
        enemy.Hearing.EnemyHeardFromPeace = false;
        OnShot(enemy, distance, true);
    }

    /// <summary>
    /// A bot was hit by an unseen shooter: nearby squadmates that know the shooter and are not reacting already get the vague area and roll as for a close bullet.
    /// </summary>
    /// <summary>
    /// The bot's enemy record for the shooter. If the bot's group does not know the shooter yet (SAIN then cannot make a record, vanilla refuses), the alert tells it,
    /// as the vanilla radio did: the shooter becomes a group enemy and the record follows.
    /// </summary>
    public static Enemy EnemyFor(BotComponent bot, IPlayer shooter)
    {
        Enemy found = bot.EnemyController.GetEnemy(shooter.ProfileId, true) ?? bot.EnemyController.CheckAddEnemy(shooter);
        if (found == null)
        {
            bot.BotOwner?.BotsGroup?.CheckAndAddEnemy(shooter, true);
            found = bot.EnemyController.GetEnemy(shooter.ProfileId, false) ?? bot.EnemyController.CheckAddEnemy(shooter);
        }
        return found;
    }

    private void AlertSquad(IPlayer shooter, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.ShotSquadAlertEnabled || _vaguePositionTime <= 0f || Bot.Squad?.Members == null)
        {
            return;
        }
        float rangeSqr = fork.ShotSquadRoleRange * fork.ShotSquadRoleRange;
        foreach (var member in Bot.Squad.Members.Values)
        {
            if (member == null || member == Bot || member.BotOwner == null || member.BotOwner.IsDead || member.ShotReaction == null)
            {
                continue;
            }
            if ((member.Position - Bot.Position).sqrMagnitude > rangeSqr)
            {
                continue;
            }
            Enemy mateEnemy = EnemyFor(member, shooter);
            if (mateEnemy != null)
            {
                member.ShotReaction.QueueSquadAlert(mateEnemy, _vaguePosition, Bot.name, Random.Range(fork.ShotSquadAlertDelayMin, Mathf.Max(fork.ShotSquadAlertDelayMin, fork.ShotSquadAlertDelayMax)), _vagueUncertainty, Bot.Position);
            }
        }
    }

    /// <summary>
    /// The bot learns a rough area from a mate's alert (hit, kill or bullet). The alert carries the source's area (point and radius): this bot takes it as it came, blurred only by
    /// the distance to the source (TakeShared), not re-guessed from its own distance. A man-down alert (center given) is still drawn around that center with the larger of the
    /// bot's own and the alert's error.
    /// </summary>
    private void LearnFromAlert(Enemy enemy, float alertUncertainty, Vector3? center = null, Vector3? area = null, Vector3? sourcePosition = null)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (center == null && area.HasValue && sourcePosition.HasValue)
        {
            NoteInformation(Time.time, PieceWeight(false));
            float distance = (sourcePosition.Value - Bot.Position).magnitude;
            Vector3 held = TakeShared(area.Value, alertUncertainty, TransmissionError(distance), -1f, "alert", _pendingAlertFrom ?? "a mate", distance);
            MarkKnown(enemy, held);
            return;
        }
        float own = ComputeUncertainty(enemy.EnemyPlayer, enemy.RealDistance, false, enemy);
        float uncertainty = Shrunk(Mathf.Max(own, alertUncertainty), NoteInformation(Time.time, PieceWeight(false)));
        float radius = Mathf.Max(Mathf.Min(uncertainty, ErrorCeiling(fork)), fork.ShotPositionErrorMin);
        Vector2 offset = RingOffset(radius * fork.ShotAreaInnerFraction, radius);
        Vector3 ringCenter = center ?? enemy.EnemyPosition;
        Vector3 point = SnapKeepingRing(ringCenter + new Vector3(offset.x, 0f, offset.y), ringCenter, radius * fork.ShotAreaInnerFraction, radius);
        MarkKnown(enemy, point);
        RememberVaguePosition(point, uncertainty);
    }

    /// <summary>A squadmate was hit by this shooter: this bot learns a rough area of its own and rolls its own reaction as for a close bullet.</summary>
    public void ReceiveSquadAlert(Enemy enemy, Vector3 area, string fromName, float uncertainty, Vector3 sourcePosition)
    {
        float now = Time.time;
        if (!AppliesTo(enemy) || Outcome != EShotReaction.None || Relocating || now < _nextRollTime)
        {
            return;
        }
        LearnFromAlert(enemy, uncertainty, null, area, sourcePosition);
        LogEvent($"squad alert from {fromName}: rolling as for a close bullet");
        Roll(enemy, enemy.RealDistance, false, now);
    }

    /// <summary>
    /// A shot was fired at the bot (it hit, or a bullet passed close) by an enemy it cannot see.
    /// </summary>
    public void OnShot(Enemy enemy, float distance, bool hit)
    {
        if (_deathHandled || Bot.BotOwner == null || Bot.BotOwner.IsDead)
        {
            return;
        }
        if (!hit)
        {
            // A close bullet counts toward a duel retreat even when the bot sees the shooter (a hit is counted in OnHit).
            NoteDuelHit(enemy, false);
        }
        if (!AppliesTo(enemy))
        {
            LogSkipped(enemy, hit);
            return;
        }
        if (TryHoldCallIn(enemy, distance, hit))
        {
            return;
        }
        ProcessShot(enemy, distance, hit);
    }

    /// <summary>
    /// Fork: a Rogue does not react to a hit or a close bullet at once. The reaction (run for cover) and the radio call go out together after a short wait
    /// (hit: a fraction of a second, bullet: a second or two). A Rogue that dies in the wait makes no call: the kill path decides (witness or man-down).
    /// Shots while a reaction is already running are not held.
    /// </summary>
    private bool TryHoldCallIn(Enemy enemy, float distance, bool hit)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.RogueCallInDelayed || Bot.Info.Profile.WildSpawnType != WildSpawnType.exUsec || Outcome != EShotReaction.None || Relocating)
        {
            return false;
        }
        float now = Time.time;
        float min = hit ? fork.RogueHitCallInMin : fork.RogueFlyByCallInMin;
        float max = hit ? fork.RogueHitCallInMax : fork.RogueFlyByCallInMax;
        float delay = Random.Range(min, Mathf.Max(min, max));
        if (_callInPending)
        {
            _callInTime = Mathf.Min(_callInTime, now + delay);
            _callInHit |= hit;
            _callInEnemy = enemy;
            _callInDistance = distance;
            return true;
        }
        _callInPending = true;
        _callInTime = now + delay;
        _callInHit = hit;
        _callInEnemy = enemy;
        _callInDistance = distance;
        LogEvent($"{(hit ? "hit" : "bullet")} at {distance:0} m: reacts and radios in {delay:0.0} s");
        return true;
    }

    private void ProcessCallIn(float now)
    {
        if (!_callInPending || now < _callInTime)
        {
            return;
        }
        _callInPending = false;
        Enemy enemy = _callInEnemy;
        _callInEnemy = null;
        if (_deathHandled || Bot.BotOwner == null || Bot.BotOwner.IsDead)
        {
            return;
        }
        if (!AppliesTo(enemy))
        {
            LogEvent("call-in dropped: the shooter is seen by now, normal combat");
            return;
        }
        ProcessShot(enemy, _callInDistance, _callInHit);
    }

    /// <summary>
    /// How close (meters) a bullet has to pass for the shot reaction to count it as a close bullet: the fork's near-hit distance for every bot with the reaction on
    /// (the game only reports bullets within 10 m). Gunners on a mounted gun have their own, wider range (MountedFlyByRange).
    /// </summary>
    public float NearHitDistance
    {
        get { return Enabled ? GlobalSettings.Fork.ShotNearHitDistance : 0f; }
    }

    /// <summary>A close bullet adds to the pressure on this bot (see TrySelfPreservation); the pressure is forgotten after a quiet spell.</summary>
    private void NoteBulletPressure(float now)
    {
        if (now - _bulletStackTime > GlobalSettings.Fork.ShotBulletStackReset)
        {
            _bulletStack = 0;
        }
        _bulletStack++;
        _bulletStackTime = now;
        _localPressureTime = now;
    }

    /// <summary>A hit or close bullet at a Rogue or Goon adds to the pressure of the Rogues and Goons within AreaShareRadius of it too (the same distance that shares the narrowing).</summary>
    private void ShareBulletPressure(float now, bool hit)
    {
        float radius = GlobalSettings.Fork.AreaShareRadius;
        if (radius <= 0f || !IsRogueOrGoon(Bot.BotOwner))
        {
            return;
        }
        BotManagerComponent manager = BotManagerComponent.Instance;
        if (manager?.Bots == null)
        {
            return;
        }
        float radiusSqr = radius * radius;
        foreach (BotComponent other in manager.Bots.Values)
        {
            if (other == null || other == Bot || other.BotOwner == null || other.BotOwner.IsDead || other.ShotReaction == null || !IsRogueOrGoon(other.BotOwner))
            {
                continue;
            }
            if ((other.Position - Bot.Position).sqrMagnitude <= radiusSqr)
            {
                if (hit)
                {
                    other.ShotReaction._localPressureTime = now;
                }
                else
                {
                    other.ShotReaction.NoteBulletPressure(now);
                }
            }
        }
    }

    private void ProcessShot(Enemy enemy, float distance, bool hit)
    {
        float now = Time.time;
        if (!hit)
        {
            NoteBulletPressure(now);
        }
        else
        {
            _localPressureTime = now;
        }
        ShareBulletPressure(now, hit);
        AlertRogues(enemy.EnemyPlayer, hit, now);
        if (TryStationaryGate(enemy, hit, now))
        {
            return;
        }
        _safetyBeforeShot = Safety(now, true);
        _lastThreatTime = now;
        _safetyCacheTime = -999f;
        _lastShotDistance = distance;
        if (hit)
        {
            _hitTimes.Enqueue(now);
        }
        // A state is "in effect" until it expires (hide, fight, fight on, investigate) or while the bot retreats. A shot then never starts a new roll.
        bool inEffect = (Outcome != EShotReaction.None && now < _expireTime) || (Relocating && enemy == _enemy);
        if (enemy == _enemy && (inEffect || now < _nextRollTime))
        {
            // Interrupts: a shooter at point blank, or a bot that keeps getting hit while it hides or runs, fights.
            if (TryShotInterrupt(enemy, distance, hit, now))
            {
                return;
            }
            // Same fight: the shot only keeps the reaction going.
            string state = Relocating ? "Retreat" : Outcome.ToString();
            if (Outcome != EShotReaction.None)
            {
                if (Outcome == EShotReaction.Cover && !_followDone && now < _hideUntil)
                {
                    // Shot at again while hiding: it keeps hiding a bit longer.
                    _hideUntil = Mathf.Max(_hideUntil, now + HIDE_EXTEND);
                    _expireTime = Mathf.Max(_expireTime, _hideUntil + 0.5f);
                }
                else
                {
                    _expireTime = Mathf.Max(_expireTime, now + 0.5f);
                }
            }
            LogEvent($"{(hit ? "hit" : "bullet")} at {distance:0} m while {state} is in effect, no new roll");
            if (TryRepeatRelocation(hit, distance, now))
            {
                return;
            }
            if (TryBreakInvestigate(enemy, distance, hit, now))
            {
                return;
            }
            TrySelfPreservation(hit, now, distance);
            return;
        }
        Roll(enemy, distance, hit, now);
        if (hit)
        {
            AlertSquad(enemy.EnemyPlayer, now);
        }
    }

    // ---- Mounted guns (Rogues) ----

    /// <summary>True while this bot has left its mounted gun because of a shot: the vanilla gun layer must not take it back (see StationaryPatches).</summary>
    public bool StationaryBlocked
    {
        get { return Time.time < _stationaryBlockUntil; }
    }

    private BotStationaryWeaponData StationaryData
    {
        get { return Bot.BotOwner?.WeaponManager?.Stationary; }
    }

    /// <summary>The bot is on a mounted gun, or on its way to one.</summary>
    private bool OnStationary()
    {
        BotStationaryWeaponData stationary = StationaryData;
        return !StationaryBlocked && stationary != null && stationary.CurLink != null && (stationary.Taken || stationary.WantToTake);
    }

    /// <summary>
    /// A shot at a bot on a mounted gun. From inside the gun's sector the bot stays and the gun answers (true = no reaction roll);
    /// from outside it the gun is useless, the bot leaves it and reacts like any other bot (false = go on to the roll).
    /// </summary>
    private bool TryStationaryGate(Enemy enemy, bool hit, float now)
    {
        if (!GlobalSettings.Fork.ShotStationaryEnabled || !OnStationary())
        {
            return false;
        }
        BotStationaryWeaponData stationary = StationaryData;
        if (!stationary.Taken)
        {
            // Assigned to a gun (or walking to it) but not seated: it cannot answer from there, so it takes cover at once instead of walking on to the gun under fire.
            AbandonStationary(stationary, now, $"{(hit ? "hit" : "bullet")} while the bot is not seated on its gun yet (cannot answer)");
            return false;
        }
        // The direction of a shot is known to the bot (not the distance): the true shooter position is used for the sector test.
        if (stationary.IsEnemyAtSector(stationary.CurLink, enemy.EnemyPosition))
        {
            string unable = UnableToAnswer(stationary, enemy.EnemyPosition, enemy.IsVisible);
            if (unable == null && GunCanAnswer(now))
            {
                if (now >= _nextStationaryLog)
                {
                    _nextStationaryLog = now + 3f;
                    LogEvent($"{(hit ? "hit" : "bullet")} from inside the sector of the mounted gun: stays on the gun, no reaction roll");
                }
                return true;
            }
            AbandonStationary(stationary, now, unable != null
                ? $"{(hit ? "hit" : "bullet")} inside the sector, but {unable}"
                : $"{(hit ? "hit" : "bullet")} inside the sector, but the gun has not fired for {GlobalSettings.Fork.ShotStationaryAnswerGrace:0} s (cannot answer, last trigger pull {_lastTriggerAge:0} s ago)");
            return false;
        }
        AbandonStationary(stationary, now, $"{(hit ? "hit" : "bullet")} from outside the sector of the mounted gun");
        return false;
    }

    /// <summary>
    /// A gunner whose gun covers the shot stays only while it can answer: it fired (a trigger pull, suppression included) within the grace time of the next shot or alert.
    /// The first shot inside the arc starts the grace window; a trigger pull restarts it.
    /// </summary>
    private bool GunCanAnswer(float now)
    {
        float grace = GlobalSettings.Fork.ShotStationaryAnswerGrace;
        if (grace <= 0f)
        {
            return true;
        }
        if (now - _lastStayEvent > 20f)
        {
            _stayStart = -1f;
        }
        _lastStayEvent = now;
        float lastTrigger = Bot.BotOwner.ShootData != null ? Bot.BotOwner.ShootData.LastTriggerPressd : -999f;
        _lastTriggerAge = now - lastTrigger;
        // A suppression burst started by TickAreaSuppression counts as answering for its length plus the grace time (the vanilla trigger time is not trusted for it).
        bool firing = now - lastTrigger < grace || now < _answeringUntil;
        if (_stayStart < 0f || firing)
        {
            _stayStart = now;
            return true;
        }
        return now - _stayStart < grace;
    }

    private void AbandonStationary(BotStationaryWeaponData stationary, float now, string what)
    {
        float hold = Mathf.Max(GlobalSettings.Fork.ShotStationaryHold, 0f);
        _stationaryBlockUntil = now + hold;
        _stayStart = -1f;
        if (stationary.Taken)
        {
            stationary.DropCurWeapon(true, true);
        }
        else if (stationary.CurLink != null)
        {
            stationary.WantToTake = false;
            stationary.SetCurLink(null);
        }
        LogEvent($"{what}: leaves the post (kept off it for {hold:0} s)");
    }

    /// <summary>A Rogue was hit by an unseen shooter: every other Rogue in range learns the vague area (staggered) and takes cover, unless its mounted gun covers that area.</summary>
    private void AlertRogues(IPlayer shooter, bool hit, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.ShotRogueAlertEnabled || Bot.Info.Profile.WildSpawnType != WildSpawnType.exUsec || now - _vaguePositionTime > VAGUE_POSITION_MAX_AGE || now - _lastRogueAlertTime < 5f)
        {
            return;
        }
        if (BotManagerComponent.Instance?.Bots == null)
        {
            return;
        }
        _lastRogueAlertTime = now;
        _lastAlertSent = now;
        int alerted = FanOutRogueAlert(shooter, Bot.Position, _vaguePosition, _vagueUncertainty, Bot.name, Bot, null, false);
        string rangeText = fork.ShotRogueAlertRange <= 0f ? "anywhere on the map" : $"within {fork.ShotRogueAlertRange:0} m";
        LogEvent($"rogue alert ({(hit ? "hit" : "bullet")}): {alerted} other Rogue(s) {rangeText} take the alert (chance {fork.ShotRogueAlertChance:0.00} each, delay grows with distance)");
    }

    /// <summary>
    /// Queues the alert at every other living Rogue in range (each with its own chance and delay from its distance to origin).
    /// center = where the alert's area is centred (null = around the shooter, the usual rough shot area); manDown = the Rogues cover whatever their gun could answer.
    /// A real alert makes a pending man-down timer pointless (everybody is alerted anyway).
    /// </summary>
    private static int FanOutRogueAlert(IPlayer shooter, Vector3 origin, Vector3 area, float uncertainty, string fromName, BotComponent exclude, Vector3? center, bool manDown)
    {
        long started = ShotPerf.Start();
        int alerted = FanOutCore(shooter, origin, area, uncertainty, fromName, exclude, center, manDown);
        ShotPerf.Stop(ShotPerf.FanOut, started);
        return alerted;
    }

    private static int FanOutCore(IPlayer shooter, Vector3 origin, Vector3 area, float uncertainty, string fromName, BotComponent exclude, Vector3? center, bool manDown)
    {
        ForkSettings fork = GlobalSettings.Fork;
        BotManagerComponent manager = BotManagerComponent.Instance;
        if (manager?.Bots == null || shooter == null)
        {
            return 0;
        }
        if (!manDown)
        {
            _deferred.RemoveAll(d => d.ManDown);
        }
        bool unlimited = fork.ShotRogueAlertRange <= 0f;
        float rangeSqr = fork.ShotRogueAlertRange * fork.ShotRogueAlertRange;
        int alerted = 0;
        foreach (BotComponent other in manager.Bots.Values)
        {
            if (other == null || other == exclude || other.BotOwner == null || other.BotOwner.IsDead || other.ShotReaction == null)
            {
                continue;
            }
            float awaySqr = (other.Position - origin).sqrMagnitude;
            if (other.Info.Profile.WildSpawnType != WildSpawnType.exUsec || (!unlimited && awaySqr > rangeSqr))
            {
                continue;
            }
            if (Random.value >= fork.ShotRogueAlertChance)
            {
                continue;
            }
            // A Rogue that never saw the shooter has no enemy record for it: the alert makes one (see EnemyFor), it does not depend on having been seen.
            Enemy otherEnemy = EnemyFor(other, shooter);
            if (otherEnemy != null)
            {
                float delay = Random.Range(fork.ShotRogueAlertDelayMin, Mathf.Max(fork.ShotRogueAlertDelayMin, fork.ShotRogueAlertDelayMax)) + Mathf.Sqrt(awaySqr) * fork.ShotRogueAlertDelayPerMeter;
                other.ShotReaction.QueueRogueAlert(otherEnemy, area, fromName, delay, uncertainty, origin, center, manDown);
                alerted++;
            }
        }
        return alerted;
    }

    /// <summary>A radio call that goes out later, from nobody in particular (the Rogue that would have made it is dead). Ticked by any living Rogue.</summary>
    private sealed class DeferredRadio
    {
        public float Due;
        public IPlayer Shooter;
        public Vector3 Origin;
        public Vector3 Area;
        public float Uncertainty;
        public string From;
        public bool ManDown;
    }

    private static readonly List<DeferredRadio> _deferred = [];
    private static int _deferredFrame = -1;

    private static void LogRadio(string message)
    {
        if (GlobalSettings.Fork.ShotReactionLog)
        {
            global::SAIN.Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} [RogueRadio] {message}");
        }
    }

    private static void ProcessDeferredRadio(float now)
    {
        if (_deferred.Count == 0 || _deferredFrame == Time.frameCount)
        {
            return;
        }
        _deferredFrame = Time.frameCount;
        List<DeferredRadio> due = null;
        for (int i = _deferred.Count - 1; i >= 0; i--)
        {
            if (now >= _deferred[i].Due)
            {
                (due ??= []).Add(_deferred[i]);
                _deferred.RemoveAt(i);
            }
        }
        if (due == null)
        {
            return;
        }
        foreach (DeferredRadio d in due)
        {
            if (now - d.Due > 60f)
            {
                continue;
            }
            int alerted = FanOutRogueAlert(d.Shooter, d.Origin, d.Area, d.Uncertainty, d.From, null, d.ManDown ? d.Area : (Vector3?)null, d.ManDown);
            LogRadio($"{(d.ManDown ? "man-down call" : "call of the witness")} for {d.From}: {alerted} Rogue(s) take the alert{(d.ManDown ? " (they cover, no position, no suppression)" : "")}");
        }
    }

    /// <summary>
    /// A living Rogue that could have seen the victim fall: within its own visible distance, inside its own visible sector (its preset angle and look direction), and a clear line from its eyes to the body.
    /// Only Rogues in range get the raycast.
    /// </summary>
    private static BotComponent FindWitness(Player victim)
    {
        BotManagerComponent manager = BotManagerComponent.Instance;
        if (manager?.Bots == null || victim == null)
        {
            return null;
        }
        Vector3 body = victim.Position + (Vector3.up * 0.9f);
        foreach (BotComponent other in manager.Bots.Values)
        {
            if (other == null || other.BotOwner == null || other.BotOwner.IsDead || other.BotOwner.GetPlayer == victim || other.Info.Profile.WildSpawnType != WildSpawnType.exUsec)
            {
                continue;
            }
            LookSensor look = other.BotOwner.LookSensor;
            if (look == null)
            {
                continue;
            }
            float sight = look.VisibleDist;
            if ((other.Position - victim.Position).sqrMagnitude > sight * sight)
            {
                continue;
            }
            if (!look.IsPointInVisibleSector(body))
            {
                continue;
            }
            Vector3 eyes = other.Transform.EyePosition;
            if (Physics.Linecast(eyes, body, LayersMaskController.HighPolyWithTerrainMaskAI))
            {
                continue;
            }
            return other;
        }
        return null;
    }

    /// <summary>
    /// A Rogue was killed (an instant kill never reaches the hit reaction; one that already radioed a hit or bullet moments ago is not alerted twice).
    /// Seen by a Rogue: it radios after a short wait, with the rough area around the killer. Not seen: nobody radios, until a long wait ends in a man-down call (no position).
    /// </summary>
    private void RogueKillAlert(Player victim, IPlayer killer, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (now - _lastAlertSent < ALERT_SENT_WINDOW)
        {
            LogEvent("kill: this Rogue radioed a moment ago, no second alert");
            return;
        }
        if (!fork.RogueKillNeedsWitness)
        {
            _lastRogueAlertTime = -999f;
            AlertRogues(killer, true, now);
            return;
        }
        Vector3 corpse = victim.Position;
        BotComponent witness = FindWitness(victim);
        if (witness != null)
        {
            float delay = Random.Range(fork.RogueKillWitnessDelayMin, Mathf.Max(fork.RogueKillWitnessDelayMin, fork.RogueKillWitnessDelayMax));
            _deferred.Add(new DeferredRadio { Due = now + delay, Shooter = killer, Origin = corpse, Area = _vaguePosition, Uncertainty = _vagueUncertainty, From = Bot.name, ManDown = false });
            LogEvent($"kill seen by {witness.name}: the radio call goes out in {delay:0.0} s");
            return;
        }
        if (_deferred.Exists(d => d.ManDown))
        {
            LogEvent("kill not seen by any Rogue: a man-down call is already pending, no second timer");
            return;
        }
        float wait = Random.Range(fork.RogueManDownDelayMin, Mathf.Max(fork.RogueManDownDelayMin, fork.RogueManDownDelayMax));
        _deferred.Add(new DeferredRadio { Due = now + wait, Shooter = killer, Origin = corpse, Area = corpse, Uncertainty = fork.ShotPositionErrorMax, From = Bot.name, ManDown = true });
        LogEvent($"kill not seen by any Rogue: nobody radios it, man-down call in {wait:0} s");
    }

    /// <summary>The squadmates of a bot that was killed by the player (within the squad role range) have lost one of their own: see NoteSquadLoss.</summary>
    private void NoteSquadLossForMates(IPlayer killer)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (killer == null || Bot.Squad?.Members == null || (fork.ShotReactionPlayerOnly && (killer as Player)?.IsYourPlayer != true))
        {
            return;
        }
        float rangeSqr = fork.ShotSquadRoleRange * fork.ShotSquadRoleRange;
        float now = Time.time;
        foreach (var member in Bot.Squad.Members.Values)
        {
            if (member == null || member == Bot || member.BotOwner == null || member.BotOwner.IsDead || member.ShotReaction == null)
            {
                continue;
            }
            if ((member.Position - Bot.Position).sqrMagnitude <= rangeSqr)
            {
                member.ShotReaction.NoteSquadLoss(now);
            }
        }
    }

    /// <summary>
    /// A bot was killed (an instant kill never reaches the hit reaction): a Rogue's death goes through the witness rules (see RogueKillAlert), any other bot alerts its squadmates as for a hit,
    /// both with a vague area around the killer.
    /// </summary>
    private void OnOwnDeath(Player player, IPlayer lastAggressor, DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
    {
        try
        {
            if (_deathHandled)
            {
                return;
            }
            _deathHandled = true;
            // A call-in the Rogue did not live to make.
            _callInPending = false;
            ForkSettings fork = GlobalSettings.Fork;
            NoteSquadLossForMates(lastAggressor);
            bool rogue = Bot.Info.Profile.WildSpawnType == WildSpawnType.exUsec;
            bool wanted = rogue ? fork.ShotRogueAlertEnabled : (fork.ShotSquadAlertEnabled && Enabled);
            if (lastAggressor == null || !wanted)
            {
                if (rogue)
                {
                    LogEvent("killed: no alert (alert switched off or no killer)");
                }
                return;
            }
            // The kill alert works from the killer alone: the victim often never saw the killer and has no enemy record for it.
            string killerName = lastAggressor.Profile?.Nickname;
            if (fork.ShotReactionPlayerOnly && (lastAggressor as Player)?.IsYourPlayer != true)
            {
                if (rogue)
                {
                    LogEvent($"killed by {killerName}, not you: no alert");
                }
                return;
            }
            Vector3 killer = lastAggressor.Position;
            float distance = (killer - Bot.Position).magnitude;
            float uncertainty = ComputeUncertainty(lastAggressor, distance, true, null);
            float radius = Mathf.Max(Mathf.Min(uncertainty, ErrorCeiling(fork)), fork.ShotPositionErrorMin);
            Vector2 offset = RingOffset(radius * fork.ShotAreaInnerFraction, radius);
            RememberVaguePosition(killer + new Vector3(offset.x, 0f, offset.y), uncertainty);
            LogEvent($"killed by {killerName} from {distance:0} m (uncertainty {uncertainty:0} m)");
            if (rogue)
            {
                RogueKillAlert(player, lastAggressor, Time.time);
            }
            else
            {
                AlertSquad(lastAggressor, Time.time);
            }
        }
        catch (System.Exception ex)
        {
            LogEvent($"death alert failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Why a gunner whose gun covers the shot still cannot answer it (null = it can): the target is beyond the launcher's range cap, or it cannot pinpoint the shooter
    /// (the uncertainty of the rough area is above the limit of its gun). A target it sees is always answered.
    /// </summary>
    /// <summary>
    /// A gunner judges a sighting another bot reported with a multiple of its error (a mounted gun has no real scope). Read when the gunner decides, not when the report arrived,
    /// so a bot that left its gun and takes one again is judged the same way. 1 for any other kind of information (a shot alert, its own senses).
    /// </summary>
    private float GunnerErrorMultiplier()
    {
        return _vagueSighting ? Mathf.Max(1f, GlobalSettings.Fork.MountedSightingErrorMultiplier) : 1f;
    }

    private string UnableToAnswer(BotStationaryWeaponData stationary, Vector3 point, bool visible)
    {
        if (visible || stationary.CurLink == null)
        {
            return null;
        }
        ForkSettings fork = GlobalSettings.Fork;
        if (LauncherBeyondRange(stationary, point, false))
        {
            return "beyond the grenade launcher's range (cannot reach)";
        }
        float max = stationary.CurLink.IsGrenade() ? fork.MountedGrenadeMaxUncertainty : fork.MountedMgMaxUncertainty;
        float multiplier = GunnerErrorMultiplier();
        float uncertainty = FreshUncertainty * multiplier;
        if (max > 0f && uncertainty > max)
        {
            return $"too unsure where the shot came from (uncertainty {uncertainty:0} m{(multiplier > 1f ? $" = {FreshUncertainty:0} m x{multiplier:0.0} for a mounted gun" : "")} > {max:0} m, cannot pinpoint)";
        }
        return null;
    }

    /// <summary>A grenade launcher cannot reach a point farther than the range cap unless its target is seen.</summary>
    private bool LauncherBeyondRange(BotStationaryWeaponData stationary, Vector3 point, bool visible)
    {
        float cap = GlobalSettings.Fork.MountedGrenadeMaxRange;
        return cap > 0f && stationary.CurLink != null && stationary.CurLink.IsGrenade() && !visible && (point - Bot.Position).magnitude > cap;
    }

    private void AreaDiag(float now, string why)
    {
        if (now >= _nextAreaDiag)
        {
            _nextAreaDiag = now + 5f;
            LogEvent($"area suppression not started: {why}");
        }
    }

    /// <summary>
    /// Bullets near a Rogue on a mounted gun: the flyby distance that counts as "shot at" is wider than for other bots (0 = same as others).
    /// </summary>
    public float MountedFlyByRange
    {
        get
        {
            if (!Enabled || Bot.Info.Profile.WildSpawnType != WildSpawnType.exUsec || StationaryData?.Taken != true)
            {
                return 0f;
            }
            return GlobalSettings.Fork.ShotMountedUnderFireDistance;
        }
    }

    /// <summary>
    /// A Rogue that never saw the player has no enemy record, so a hit or a close bullet would be dropped before the reaction sees it.
    /// For a Rogue (and the shooter the reaction applies to) the shot makes it aware of the shooter, which gives no position, only the record.
    /// </summary>
    public Enemy AwareOf(IPlayer shooter)
    {
        if (shooter == null || !Enabled || Bot.Info.Profile.WildSpawnType != WildSpawnType.exUsec)
        {
            return null;
        }
        if (GlobalSettings.Fork.ShotReactionPlayerOnly && (shooter as Player)?.IsYourPlayer != true)
        {
            return null;
        }
        return EnemyFor(Bot, shooter);
    }

    /// <summary>A bullet of the shooter landed at impactPosition. For a Rogue on a mounted gun, an impact close to it counts as a close bullet.</summary>
    public void OnBulletImpact(IPlayer shooter, Vector3 impactPosition)
    {
        float range = GlobalSettings.Fork.ShotMountedImpactRange;
        if (range <= 0f || shooter == null || !Enabled || Bot.Info.Profile.WildSpawnType != WildSpawnType.exUsec || StationaryData?.Taken != true)
        {
            return;
        }
        float distanceToImpact = (impactPosition - Bot.Position).magnitude;
        float now = Time.time;
        if (distanceToImpact > range || now < _nextImpactReact)
        {
            return;
        }
        Enemy enemy = AwareOf(shooter);
        if (enemy == null)
        {
            return;
        }
        _nextImpactReact = now + 1f;
        float distance = enemy.RealDistance;
        DegradePosition(enemy, enemy.EnemyPosition, distance, false);
        LogEvent($"bullet impact {distanceToImpact:0} m from the bot counts as a close bullet");
        OnShot(enemy, distance, false);
    }

    private sealed class AreaTarget
    {
        public Vector3 Area;
        public float Radius;
        public BotOwner Owner;
        public bool Grenade;
        private float _next;
        private Vector2 _offset;

        public Vector3 Get()
        {
            // A launcher ranges in across its bursts: the jitter around the rough area is wide for its first grenades and narrows with every one (also counts the shots).
            float ranging = Grenade ? MountedHelpers.RangingMultiplier(Owner) : 1f;
            if (Time.time >= _next)
            {
                _next = Time.time + 1f;
                _offset = Random.insideUnitCircle * Radius * ranging;
            }
            return Area + new Vector3(_offset.x, 0f, _offset.y);
        }
    }

    private float _nextAreaCheck;
    private float _areaDelayUntil = -1f;
    private float _lastAreaActivity = -999f;
    private float _nextAreaSuppress;

    /// <summary>
    /// A Rogue on a mounted gun that knows only a rough shot area (and cannot see the player) fires suppression bursts at that area through the vanilla suppression layer.
    /// Random: a chance roll per check, random burst length, random pauses, a target that jitters inside an error radius. Never uses the player's live position.
    /// </summary>
    private void TickAreaSuppression(float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.MountedAreaSuppress || now < _nextAreaCheck)
        {
            return;
        }
        _nextAreaCheck = now + Random.Range(0.7f, 1.5f);
        if (Bot.Info.Profile.WildSpawnType != WildSpawnType.exUsec || now - _vaguePositionTime > fork.MountedAreaSuppressAreaAge || now < _nextAreaSuppress || StationaryBlocked)
        {
            return;
        }
        BotStationaryWeaponData stationary = StationaryData;
        if (stationary == null || stationary.CurLink == null)
        {
            return;
        }
        if (!stationary.Taken)
        {
            AreaDiag(now, "the bot is not seated on its gun yet");
            return;
        }
        BotSuppressStationary suppress = Bot.BotOwner.SuppressStationary;
        AbstractSuppressStationary logic = suppress?.CurUsingLogic;
        if (logic == null)
        {
            AreaDiag(now, "the bot has no suppression logic");
            return;
        }
        if (logic.IsInProcess)
        {
            return;
        }
        Enemy goal = Bot.GoalEnemy;
        if (goal != null && goal.IsVisible && goal.CanShoot)
        {
            AreaDiag(now, "the bot sees its target (normal shooting)");
            return;
        }
        // After a quiet spell the first burst waits a random start delay (a gunner does not answer the first shot at once).
        if (now - _lastAreaActivity > 45f)
        {
            _areaDelayUntil = now + Random.Range(fork.MountedAreaStartDelayMin, Mathf.Max(fork.MountedAreaStartDelayMin, fork.MountedAreaStartDelayMax));
        }
        _lastAreaActivity = now;
        if (now < _areaDelayUntil)
        {
            return;
        }
        if (Random.value >= fork.MountedAreaSuppressChance)
        {
            _nextAreaSuppress = now + Random.Range(2f, 5f);
            return;
        }
        bool grenade = stationary.CurLink.IsGrenade();
        float maxUncertainty = grenade ? fork.MountedGrenadeMaxUncertainty : fork.MountedMgMaxUncertainty;
        float gunnerError = _vagueUncertainty * GunnerErrorMultiplier();
        if (maxUncertainty > 0f && gunnerError > maxUncertainty)
        {
            AreaDiag(now, $"the area is too uncertain ({gunnerError:0} m{(gunnerError > _vagueUncertainty + 0.01f ? $" = {_vagueUncertainty:0} m x{GunnerErrorMultiplier():0.0} for a mounted gun" : "")} > {maxUncertainty:0} m): the gun cannot pinpoint it");
            _nextAreaSuppress = now + 10f;
            return;
        }
        if (grenade && LauncherBeyondRange(stationary, _vaguePosition, false))
        {
            AreaDiag(now, "the area is beyond the grenade launcher's range cap");
            _nextAreaSuppress = now + 10f;
            return;
        }
        float length = grenade
            ? Random.Range(fork.MountedGrenadeBurstMin, Mathf.Max(fork.MountedGrenadeBurstMin, fork.MountedGrenadeBurstMax))
            : Random.Range(fork.MountedMgBurstMin, Mathf.Max(fork.MountedMgBurstMin, fork.MountedMgBurstMax));
        float pause = grenade
            ? Random.Range(fork.MountedGrenadePauseMin, Mathf.Max(fork.MountedGrenadePauseMin, fork.MountedGrenadePauseMax))
            : Random.Range(fork.MountedAreaCooldownMin, Mathf.Max(fork.MountedAreaCooldownMin, fork.MountedAreaCooldownMax));
        var target = new AreaTarget { Area = _vaguePosition, Radius = fork.MountedAreaSuppressError, Owner = Bot.BotOwner, Grenade = grenade };
        if (!logic.CanStartSuppressAt(target.Area))
        {
            // Out of the gun's arc (or a spot it already wasted a burst on): not this time.
            AreaDiag(now, "the area is outside the gun's arc or a spot it already wasted a burst on");
            _nextAreaSuppress = now + pause;
            return;
        }
        suppress.SetTargetGetter(target.Get);
        logic.SetPeriod(length);
        _nextAreaSuppress = now + length + pause;
        _answeringUntil = now + length + fork.ShotStationaryAnswerGrace;
        LogEvent($"area suppression: {(grenade ? "grenade launcher" : "machine gun")} fires at the rough shot area ({(target.Area - Bot.Position).magnitude:0} m, uncertainty {_vagueUncertainty:0} m, error {target.Radius:0} m) for {length:0.0} s, then pauses {pause:0} s");
    }

    private struct PendingReport
    {
        public IPlayer Enemy;
        public Vector3 Position;
        public Vector3 WeaponRoot;
        public float Time;
        public string From;
        public BotOwner Reporter;
        public BotsGroup Group;
        public EEnemyPartVisibleType Type;
    }

    private readonly List<PendingReport> _reports = [];

    /// <summary>Another Rogue's enemy report is delivered later (see RogueReport_Patch).</summary>
    public void QueueReport(IPlayer enemy, Vector3 position, Vector3 weaponRoot, float deliverAt, string from = null, BotOwner reporter = null)
    {
        _reports.Add(new PendingReport { Enemy = enemy, Position = position, WeaponRoot = weaponRoot, Time = deliverAt, From = from, Reporter = reporter, Type = EEnemyPartVisibleType.Sence });
    }

    /// <summary>Rogues and Goons: the bots whose reports about the player are handled by the Rogue radio.</summary>
    public static bool IsRogueOrGoon(BotOwner owner)
    {
        WildSpawnType? role = owner?.Profile?.Info?.Settings?.Role;
        return role == WildSpawnType.exUsec || role == WildSpawnType.bossKnight || role == WildSpawnType.followerBigPipe || role == WildSpawnType.followerBirdEye;
    }

    /// <summary>
    /// Ordinary squads (PMCs, raiders, scavs, cultists): their sightings are shared as rough points inside the squad too (ShotSightingRingsSquads).
    /// Not bosses or followers: vanilla boss logic and other mods listen to their reports.
    /// </summary>
    public static bool IsOrdinarySquadBot(BotOwner owner)
    {
        WildSpawnType? role = owner?.Profile?.Info?.Settings?.Role;
        if (role == null)
        {
            return false;
        }
        EForkBrainGroup group = ForkBrainFilter.GetGroup(role.Value);
        return group == EForkBrainGroup.Pmc || group == EForkBrainGroup.Raider || group == EForkBrainGroup.Scav || group == EForkBrainGroup.Cultist;
    }

    private static readonly List<BotsGroup> _groupScratch = [];

    /// <summary>
    /// A Rogue or Goon sees the player. Vanilla writes the exact position into its own group at once and relays it to every Rogue and Goon group
    /// (see BotsGroup.ReportAboutEnemy). Instead, one report per Rogue/Goon group is queued here, each after its own delay (random + per metre from the reporter),
    /// and delivered as one rough point per group (see DeliverToGroup). Called at most once per interval per group.
    /// </summary>
    public void QueueGroupReports(IPlayer enemy, EEnemyPartVisibleType type, bool crossGroup = true)
    {
        ForkSettings fork = GlobalSettings.Fork;
        BotManagerComponent manager = BotManagerComponent.Instance;
        if (manager?.Bots == null || enemy == null)
        {
            return;
        }
        Vector3 position = enemy.Transform.position;
        Vector3 weaponRoot = enemy.WeaponRoot.position;
        _groupScratch.Clear();
        if (!crossGroup)
        {
            // An ordinary squad: only the reporter's own squad learns about it, after the squad alert's short delay.
            BotsGroup own = Bot.BotOwner?.BotsGroup;
            if (own != null)
            {
                _groupScratch.Add(own);
            }
        }
        else
        {
            foreach (BotComponent other in manager.Bots.Values)
            {
                if (other == null || other.BotOwner == null || other.BotOwner.IsDead || !IsRogueOrGoon(other.BotOwner))
                {
                    continue;
                }
                BotsGroup group = other.BotOwner.BotsGroup;
                if (group != null && !_groupScratch.Contains(group))
                {
                    _groupScratch.Add(group);
                }
            }
        }
        Vector3 from = Bot.Position;
        float now = Time.time;
        for (int g = 0; g < _groupScratch.Count; g++)
        {
            BotsGroup group = _groupScratch[g];
            float distance = float.MaxValue;
            for (int i = 0; i < group.MembersCount; i++)
            {
                BotOwner member = group.Member(i);
                if (member != null && member != Bot.BotOwner && !member.IsDead)
                {
                    distance = Mathf.Min(distance, (member.Position - from).magnitude);
                }
            }
            if (distance == float.MaxValue)
            {
                distance = 0f;
            }
            if (fork.RogueReportRange > 0f && distance > fork.RogueReportRange)
            {
                continue;
            }
            float delay = crossGroup
                ? Random.Range(fork.RogueReportDelayMin, Mathf.Max(fork.RogueReportDelayMin, fork.RogueReportDelayMax)) + (distance * fork.RogueReportDelayPerMeter)
                : Random.Range(fork.ShotSquadAlertDelayMin, Mathf.Max(fork.ShotSquadAlertDelayMin, fork.ShotSquadAlertDelayMax));
            _reports.Add(new PendingReport
            {
                Enemy = enemy,
                Position = position,
                WeaponRoot = weaponRoot,
                Time = now + delay,
                From = Bot.name,
                Reporter = Bot.BotOwner,
                Group = group,
                Type = type,
            });
        }
        _groupScratch.Clear();
    }

    private void ProcessReports(float now)
    {
        long budget = ShotPerf.TicksFromMs(GlobalSettings.Fork.ReportFrameBudgetMs);
        for (int i = _reports.Count - 1; i >= 0; i--)
        {
            PendingReport report = _reports[i];
            if (now < report.Time)
            {
                continue;
            }
            // Delivering a report costs a few ms, so all bots together deliver within a time budget per frame; the first one of a frame always goes through.
            if (budget > 0 && ShotPerf.FrameTicks(ShotPerf.Deliver) >= budget)
            {
                ShotPerf.ReportDeferred++;
                continue;
            }
            _reports.RemoveAt(i);
            // A reporter that died while its report was on the way sends nothing.
            if (report.Reporter != null && report.Reporter.IsDead)
            {
                continue;
            }
            long started = ShotPerf.StartAlways();
            DeliverReport(report, now);
            ShotPerf.Stop(ShotPerf.Deliver, started);
        }
    }

    /// <summary>
    /// A report from another Rogue gives this Rogue a point in a ring around the player, not the exact position. How rough it is depends on the receiver's own
    /// distance and sight (small close up, large far away) and narrows with every further piece of information (down to a distance-scaled floor).
    /// The point also becomes the rough area (with that uncertainty), so mounted guns need it narrow enough before they fire at it.
    /// </summary>
    /// <summary>
    /// The vanilla report fires many times per second while a Rogue sees the player; a receiver takes at most one per interval (the rest are dropped before any work is done).
    /// </summary>
    public bool AcceptReport()
    {
        float interval = GlobalSettings.Fork.RogueReportMinInterval;
        float now = Time.time;
        if (interval <= 0f)
        {
            return true;
        }
        if (now < _nextReportAccept)
        {
            return false;
        }
        _nextReportAccept = now + interval;
        return true;
    }

    private static readonly Dictionary<BotsGroup, float> _groupNextReport = [];

    /// <summary>
    /// One report per group per interval: the report ends in one shared record per group (BotsGroup.SetEnemyPos, the last write wins), so every member
    /// delivering its own is the same work N times and the members' points overwrite each other.
    /// </summary>
    public static bool AcceptGroupReport(BotOwner owner, ShotReactionClass fallback)
    {
        BotsGroup group = owner?.BotsGroup;
        if (group == null)
        {
            return fallback.AcceptReport();
        }
        float interval = GlobalSettings.Fork.RogueReportMinInterval;
        float now = Time.time;
        if (interval <= 0f)
        {
            return true;
        }
        if (_groupNextReport.TryGetValue(group, out float next) && now < next)
        {
            return false;
        }
        if (_groupNextReport.Count > 64)
        {
            List<BotsGroup> old = null;
            foreach (var pair in _groupNextReport)
            {
                if (pair.Value < now - 60f)
                {
                    (old ??= []).Add(pair.Key);
                }
            }
            if (old != null)
            {
                foreach (BotsGroup key in old)
                {
                    _groupNextReport.Remove(key);
                }
            }
        }
        _groupNextReport[group] = now + interval;
        return true;
    }

    /// <summary>
    /// Delivers a report for the whole group once: one rough point (as rough as the group member closest to the player would get from a shot, narrowed by this bot's count)
    /// written to the group's record, then every member (including this bot) takes the point as its rough area with its own uncertainty and count.
    /// </summary>
    private void DeliverReport(PendingReport report, float now)
    {
        BotsGroup target = report.Group ?? Bot.BotOwner?.BotsGroup;
        if (target == null)
        {
            return;
        }
        ShotReactionClass lead = this;
        if (report.Group != null)
        {
            long lookup = ShotPerf.Start();
            lead = ClosestMemberReaction(target, report.Enemy);
            ShotPerf.Stop(ShotPerf.Lookup, lookup);
            if (lead == null)
            {
                return;
            }
        }
        lead.DeliverToGroup(report, target, now);
    }

    /// <summary>The shot reaction of the group member closest to the player (the best informed one of its group).</summary>
    private static ShotReactionClass ClosestMemberReaction(BotsGroup group, IPlayer enemy)
    {
        BotManagerComponent manager = BotManagerComponent.Instance;
        if (manager == null || enemy == null)
        {
            return null;
        }
        ShotReactionClass best = null;
        float bestSqr = float.MaxValue;
        Vector3 position = enemy.Position;
        for (int i = 0; i < group.MembersCount; i++)
        {
            BotOwner member = group.Member(i);
            if (member == null || member.IsDead || !manager.GetSAIN(member, out BotComponent other) || other.ShotReaction == null)
            {
                continue;
            }
            float sqr = (position - member.Position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = other.ShotReaction;
            }
        }
        return best;
    }

    /// <summary>
    /// Delivers a report to one group (this bot is its member closest to the player): one rough point (as rough as this bot would get from a shot, narrowed by its count)
    /// written to the group's record, then every member takes the point as its rough area with its own uncertainty and count.
    /// </summary>
    private sealed class GroupPoint
    {
        public Vector3 Point;
        public float Uncertainty;
        public float Time;
        public IPlayer Enemy;
        public Vector3 SourcePosition;
        public BotOwner Source;
    }

    private static readonly Dictionary<BotsGroup, GroupPoint> _groupPoints = [];

    private void DeliverToGroup(PendingReport report, BotsGroup group, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.RogueReportRough || report.Enemy == null)
        {
            long plain = ShotPerf.Start();
            group.SetEnemyPos(report.Enemy, report.Position, report.WeaponRoot, report.Type);
            ShotPerf.Stop(ShotPerf.SetEnemyPos, plain);
            return;
        }
        float factor = NoteInformation(now);
        Vector3 playerPosition = report.Enemy.Position;
        // A report is a sighting: the error is the one of the bot that SAW the player (its distance and its sight), not of the member closest to the player.
        BotOwner source = report.Reporter != null && !report.Reporter.IsDead ? report.Reporter : Bot.BotOwner;
        ShotReactionClass sourceReaction = this;
        BotManagerComponent sourceManager = BotManagerComponent.Instance;
        if (sourceManager != null && sourceManager.GetSAIN(source, out BotComponent sourceBot) && sourceBot.ShotReaction != null)
        {
            sourceReaction = sourceBot.ShotReaction;
        }
        float distance = (playerPosition - source.Position).magnitude;
        Vector3 point;
        float uncertainty;
        Vector3 sourcePosition;
        BotOwner pointSource;
        float pointTime;
        // The group's point stays where it is until a new piece of information narrows it (at most once per cooldown): a point that jumps every report
        // makes the covers valid, then invalid, and the bots keep choosing again.
        if (_groupPoints.TryGetValue(group, out GroupPoint last) && last.Enemy == report.Enemy && now - last.Time < fork.AreaShrinkCooldown)
        {
            point = last.Point;
            uncertainty = last.Uncertainty;
            sourcePosition = last.SourcePosition;
            pointSource = last.Source;
            pointTime = last.Time;
        }
        else
        {
            long uncStart = ShotPerf.Start();
            Enemy known = sourceReaction.Bot.EnemyController?.GetEnemy(report.Enemy.ProfileId, false);
            uncertainty = Shrunk(sourceReaction.ComputeVisualUncertainty(report.Enemy, distance, known), factor);
            ShotPerf.Stop(ShotPerf.Uncertainty, uncStart);
            float radius = Mathf.Max(Mathf.Min(uncertainty, ErrorCeiling(fork)), fork.ShotPositionErrorMin);
            Vector2 offset = RingOffset(radius * fork.ShotAreaInnerFraction, radius);
            Vector3 delta = new Vector3(offset.x, 0f, offset.y);
            long snapStart = ShotPerf.Start();
            point = SnapKeepingRing(report.Position + delta, report.Position, radius * fork.ShotAreaInnerFraction, radius);
            ShotPerf.Stop(ShotPerf.Snap, snapStart);
            sourcePosition = source.Position;
            pointSource = source;
            pointTime = now;
            if (_groupPoints.Count > 32)
            {
                _groupPoints.Clear();
            }
            _groupPoints[group] = new GroupPoint { Point = point, Uncertainty = uncertainty, Time = now, Enemy = report.Enemy, SourcePosition = sourcePosition, Source = pointSource };
            if (fork.ShotReactionLog)
            {
                LogEvent($"report from {report.From}: new rough point {(point - report.Position).magnitude:0} m from the player (uncertainty {uncertainty:0} m at {distance:0} m, piece #{KnowCount}, visual)");
            }
        }
        long posStart = ShotPerf.Start();
        group.SetEnemyPos(report.Enemy, point, report.WeaponRoot + (point - report.Position), report.Type);
        ShotPerf.Stop(ShotPerf.SetEnemyPos, posStart);
        long memberStart = ShotPerf.Start();
        BotManagerComponent manager = BotManagerComponent.Instance;
        for (int i = 0; i < group.MembersCount; i++)
        {
            BotOwner member = group.Member(i);
            if (member != null && !member.IsDead && manager != null && manager.GetSAIN(member, out BotComponent other) && other.ShotReaction != null)
            {
                other.ShotReaction.TakeGroupPoint(report.Enemy, point, uncertainty, sourcePosition, pointSource, pointTime, report.From);
            }
        }
        ShotPerf.Stop(ShotPerf.Members, memberStart);
    }

    /// <summary>
    /// A member of a group takes the group's report as it came from the bot that saw the player: the same point and error, blurred only by this member's distance to that bot
    /// (the bot that saw the player itself loses nothing). The report does not depend on this bot's own distance to the player.
    /// </summary>
    public void TakeGroupPoint(IPlayer enemy, Vector3 point, float groupUncertainty, Vector3 sourcePosition, BotOwner source, float pointTime, string from)
    {
        ForkSettings fork = GlobalSettings.Fork;
        NoteInformation(Time.time);
        Enemy known = Bot.EnemyController?.GetEnemy(enemy.ProfileId, false);
        bool isSource = source != null && source == Bot.BotOwner;
        float distance = (sourcePosition - Bot.Position).magnitude;
        float transmission = isSource ? 0f : TransmissionError(distance);
        Vector3 held = TakeShared(point, groupUncertainty, transmission, pointTime, "sighting", from ?? "a mate", distance, true);
        if (fork.ShotAlertMarksKnown && (!fork.ShotReactionPlayerOnly || (enemy as Player)?.IsYourPlayer == true))
        {
            MarkKnown(known ?? EnemyFor(Bot, enemy), held);
        }
    }

    /// <summary>
    /// An alert or a report tells this bot about the player: the player becomes a known enemy at the rough point (never the exact position), as it does for a bot that was hit.
    /// SAIN's combat layer then takes over from patrol (cover, search, suppression); without this the bot only stored the area and kept walking its patrol.
    /// </summary>
    private void MarkKnown(Enemy enemy, Vector3 point)
    {
        if (enemy == null || enemy.Hearing == null || !GlobalSettings.Fork.ShotAlertMarksKnown)
        {
            return;
        }
        SAINHearingReport report = new()
        {
            position = point,
            soundType = SAINSoundType.Shot,
            placeType = EEnemyPlaceType.Hearing,
            isDanger = true,
            shallReportToSquad = false,
        };
        enemy.Hearing.SetHeard(report, Time.time);
        enemy.Hearing.EnemyHeardFromPeace = false;
    }

    /// <summary>A squadmate's hit or kill: this bot reacts after a short delay (not at once).</summary>
    public void QueueSquadAlert(Enemy enemy, Vector3 area, string fromName, float delay, float uncertainty, Vector3 sourcePosition)
    {
        if (!Enabled || _pendingAlertEnemy != null)
        {
            return;
        }
        _pendingSquad = true;
        _pendingAlertCenter = null;
        _pendingForceCover = false;
        _pendingAlertUncertainty = uncertainty;
        _pendingAlertEnemy = enemy;
        _pendingAlertArea = area;
        _pendingAlertFrom = fromName;
        _pendingAlertSource = sourcePosition;
        _pendingAlertTime = Time.time + delay;
    }

    public void QueueRogueAlert(Enemy enemy, Vector3 area, string fromName, float delay, float uncertainty, Vector3 sourcePosition, Vector3? center = null, bool forceCover = false)
    {
        if (!Enabled || _pendingAlertEnemy != null)
        {
            return;
        }
        _pendingSquad = false;
        _pendingAlertCenter = center;
        _pendingForceCover = forceCover;
        _pendingAlertUncertainty = uncertainty;
        _pendingAlertEnemy = enemy;
        _pendingAlertArea = area;
        _pendingAlertFrom = fromName;
        _pendingAlertSource = sourcePosition;
        _pendingAlertTime = Time.time + delay;
    }

    private void ProcessPendingRogueAlert(float now)
    {
        if (_pendingAlertEnemy == null || now < _pendingAlertTime)
        {
            return;
        }
        Enemy enemy = _pendingAlertEnemy;
        _pendingAlertEnemy = null;
        Vector3? alertCenter = _pendingAlertCenter;
        bool forceCover = _pendingForceCover;
        _pendingAlertCenter = null;
        _pendingForceCover = false;
        if (_pendingSquad)
        {
            if (Enemy.IsEnemyActive(enemy))
            {
                ReceiveSquadAlert(enemy, _pendingAlertArea, _pendingAlertFrom, _pendingAlertUncertainty, _pendingAlertSource);
            }
            return;
        }
        if (!Enemy.IsEnemyActive(enemy) || !AppliesTo(enemy) || Outcome != EShotReaction.None || Relocating || now < _nextRollTime)
        {
            return;
        }
        ForkSettings fork = GlobalSettings.Fork;
        LearnFromAlert(enemy, _pendingAlertUncertainty, alertCenter, _pendingAlertArea, _pendingAlertSource);
        if (forceCover && fork.ShotStationaryEnabled && OnStationary())
        {
            // Man-down: no shot area a gun could answer, so a gunner always leaves the gun for cover.
            AbandonStationary(StationaryData, now, "man-down alert, nothing the gun could answer");
        }
        else if (fork.ShotStationaryEnabled && OnStationary())
        {
            BotStationaryWeaponData stationary = StationaryData;
            bool inSector = stationary.IsEnemyAtSector(stationary.CurLink, _vaguePosition);
            if (!stationary.Taken)
            {
                AbandonStationary(stationary, now, "rogue alert while the bot is not seated on its gun yet (cannot answer)");
            }
            else
            {
                string unable = inSector ? UnableToAnswer(stationary, _vaguePosition, false) : null;
                if (inSector && unable == null && GunCanAnswer(now))
                {
                    LogEvent($"rogue alert from {_pendingAlertFrom}: the shot area is inside the sector of the mounted gun, stays on the gun");
                    return;
                }
                AbandonStationary(stationary, now, inSector
                    ? (unable != null ? $"rogue alert inside the sector, but {unable}" : $"rogue alert inside the sector, but the gun has not fired for {fork.ShotStationaryAnswerGrace:0} s (cannot answer, last trigger pull {_lastTriggerAge:0} s ago)")
                    : "rogue alert from outside the sector of the mounted gun");
            }
        }
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        LogEvent($"rogue alert from {_pendingAlertFrom}: Cover");
        Apply(enemy, EShotReaction.Cover, enemy.RealDistance / reach, now, fork);
    }

    /// <summary>
    /// While the reaction says "hide", the decision is to seek cover. False = let the normal SAIN decisions run.
    /// </summary>
    public bool TryGetDecision(Enemy enemy, out ECombatDecision decision)
    {
        decision = ECombatDecision.None;
        if (_pushUntil > 0f && TryPushDecision(enemy, Time.time, out decision))
        {
            return true;
        }
        if (Relocating)
        {
            if (enemy != _enemy || !ContinueRelocating(enemy))
            {
                return false;
            }
            decision = ECombatDecision.Relocate;
            return true;
        }
        if (!IsManaging(enemy))
        {
            LogDeclined(enemy, "not managing this enemy");
            return false;
        }
        float now = Time.time;
        if (_nadeActive)
        {
            bool throwing = Bot.BotOwner.WeaponManager?.Grenades?.ThrowindNow == true;
            if (now < _nadeUntil && (throwing || now < _nadeStart + 0.5f) && !(SeesShooter(enemy) && enemy.CanShoot))
            {
                decision = ECombatDecision.ThrowGrenade;
                return true;
            }
            _nadeActive = false;
            if (_pendingFollow.HasValue)
            {
                EFollowUp pending = _pendingFollow.Value;
                _pendingFollow = null;
                if (ApplyFollowUp(pending, now, out decision))
                {
                    return true;
                }
            }
        }
        bool seesShooter = SeesShooter(enemy) && enemy.CanShoot;
        if (_inCoverSince < 0f && Bot.Cover.CoverInUse != null)
        {
            _inCoverSince = now;
        }
        bool hiddenLongEnough = now >= _hideUntil || (_inCoverSince >= 0f && now - _inCoverSince >= RELOCATE_COVER_HOLD);
        if (!_followDone && !seesShooter && hiddenLongEnough && Outcome == EShotReaction.Cover)
        {
            // The hide is over and the bot has not seen the shooter: it fights on from cover, goes looking, or retreats. It never just resumes.
            _followDone = true;
            EFollowUp follow = ChooseFollowUp(enemy);
            // Cover first, then a grenade at the vague area, then the follow-up (retreat, fight on, investigate).
            if (TryStartNade(enemy, now, NadeChance(follow), "after the hide, before " + follow))
            {
                _pendingFollow = follow;
                decision = ECombatDecision.ThrowGrenade;
                return true;
            }
            if (ApplyFollowUp(follow, now, out decision))
            {
                return true;
            }
        }
        if (now >= _expireTime || seesShooter)
        {
            // A fight that ran out without the bot ever seeing the shooter goes on as an investigation; seeing the shooter is normal combat.
            if (seesShooter || !ChainInvestigation(now))
            {
                // Over, or the bot actually sees the shooter now: normal combat.
                End(seesShooter ? "the bot sees the shooter" : "time is up");
                return false;
            }
        }
        if (Bot.Cover.NoCoverGaveUp && Outcome != EShotReaction.Fight)
        {
            // The bot holds "cover" that does not exist (the cover finder found none, even in the wide search): the hold is over, normal combat decides.
            End("no cover to hold, gave up seeking it");
            return false;
        }
        if (_postRetreat)
        {
            UpdatePostRetreatHold(now);
        }
        if (Outcome == EShotReaction.Fight && now < _searchUntil && !seesShooter)
        {
            // Fight against a shooter it cannot see: it stops and searches the vague area (see FightSearchTick).
            decision = ECombatDecision.ShotSearch;
            return true;
        }
        if (now < _hideUntil)
        {
            decision = ECombatDecision.SeekCover;
            return true;
        }
        if (Outcome == EShotReaction.Hunt)
        {
            return TryInvestigate(enemy, now, out decision);
        }
        if (Outcome == EShotReaction.FightOn && _followDone)
        {
            // Fight On: stay in cover; SAIN's cover action fires suppressive bursts at the vague area and fights normally when it sees the shooter.
            decision = ECombatDecision.SeekCover;
            return true;
        }
        SprintWhileInvestigating = false;
        LogDeclined(enemy, "reaction has no decision left");
        return false;
    }

    private float _nextDeclinedLog;

    /// <summary>Diagnostic: a reaction that is still on (Outcome set, or a post-retreat hold) but leaves the decision to normal SAIN. One line per 3 s.</summary>
    private void LogDeclined(Enemy enemy, string why)
    {
        if (Outcome == EShotReaction.None || _nextDeclinedLog > Time.time || !GlobalSettings.Fork.ShotReactionLog)
        {
            return;
        }
        _nextDeclinedLog = Time.time + 3f;
        LogEvent($"declined to decide ({why}): outcome {Outcome}, postRetreat {_postRetreat}, hold {Mathf.Max(0f, _hideUntil - Time.time):0} s left, goal enemy {(enemy != null ? enemy.EnemyName : "none")}, reaction enemy {(_enemy != null ? _enemy.EnemyName : "none")}, cover gave up {Bot.Cover.NoCoverGaveUp}");
    }

    /// <summary>
    /// Investigate: SAIN's own search toward the vague area, sprinting. It stops when the bot is close to the area or hears the shooter:
    /// it then holds cover and watches (an ambush) for a while, and the state ends. Seeing the shooter ends it at once (handled by the caller).
    /// </summary>
    private bool TryInvestigate(Enemy enemy, float now, out ECombatDecision decision)
    {
        decision = ECombatDecision.None;
        SprintWhileInvestigating = false;
        if (_ambushUntil > 0f)
        {
            if (now >= _ambushUntil)
            {
                End("ambush over");
                return false;
            }
            decision = ECombatDecision.SeekCover;
            return true;
        }

        // Walking closer does not improve a fix that is only good to the uncertainty: it stops inside half of it.
        bool close = _vaguePositionTime > 0f && (Bot.Position - _vaguePosition).magnitude < Mathf.Max(INVESTIGATE_STOP_DISTANCE, _vagueUncertainty * 0.5f);
        bool sound = now - _investigateStart > INVESTIGATE_SOUND_GRACE && enemy.Hearing.Heard && enemy.Hearing.TimeSinceHeard < INVESTIGATE_SOUND_AGE;
        if (close || sound)
        {
            float time = Random.Range(AMBUSH_TIME_MIN, AMBUSH_TIME_MAX);
            switch (Bot.Info.Personality)
            {
                case EPersonality.Wreckless:
                case EPersonality.GigaChad:
                case EPersonality.Chad:
                    time /= GlobalSettings.Fork.ShotAggressivePull;
                    break;
                case EPersonality.Rat:
                case EPersonality.Timmy:
                case EPersonality.Coward:
                    time *= GlobalSettings.Fork.ShotTimidPull;
                    break;
            }
            _ambushUntil = now + time;
            LogEvent($"investigate stops ({(close ? "close to the shot area" : "heard the shooter")}), holding cover and watching for {time:0} s");
            decision = ECombatDecision.SeekCover;
            return true;
        }

        // Cover to cover toward the vague area: hold each cover for a few seconds (watching, firing bursts if in reach), then sprint to the next
        // cover point that is closer to the area. Without a closer point the bot holds where it is.
        Vector3 area = _vaguePosition;
        if (Bot.Cover.AdvanceActive)
        {
            float hopAge = now - Bot.Cover.AdvanceStartTime;
            bool stalled = hopAge > HOP_STALL_TIME && !Bot.Mover.Moving && Bot.Cover.CoverPoint_MovingTo == null;
            var hopTarget = Bot.Cover.CoverPoint_MovingTo;
            if (hopTarget != null)
            {
                float hopDistance = Vector3.Distance(Bot.Position, hopTarget.Position);
                if (hopDistance < _hopBestDistance - 1f)
                {
                    _hopBestDistance = hopDistance;
                    _hopProgressTime = now;
                }
            }
            ForkSettings hopFork = GlobalSettings.Fork;
            bool noProgress = now - _hopProgressTime > hopFork.ShotHopNoProgressTime;
            if (hopAge <= hopFork.ShotHopMaxTime && !stalled && !noProgress)
            {
                decision = ECombatDecision.SeekCover;
                return true;
            }
            // The hop is not getting anywhere (no path, blocked, no progress): give it up instead of standing in the open.
            LogEvent($"advance: hop abandoned ({(stalled ? "stalled" : noProgress ? "no progress" : "too long")} after {hopAge:0.0} s)");
            Bot.Cover.StopAdvance();
            _noHopUntil = now + HOP_RETRY_DELAY;
        }
        bool inCover = Bot.Cover.CoverInUse != null;
        if (inCover && Bot.Cover.TimeHeldInCover < _hopHold)
        {
            decision = ECombatDecision.SeekCover;
            return true;
        }
        if (now >= _noHopUntil && Bot.Cover.StartAdvanceHop(area))
        {
            _hopBestDistance = float.MaxValue;
            _hopProgressTime = now;
            _hopHold = HopHoldTime();
            _advanceRunUntil = 0f;
            _advanceRolled = false;
            LogEvent("advance: next cover closer to the shot area");
            decision = ECombatDecision.SeekCover;
            return true;
        }
        ForkSettings advanceFork = GlobalSettings.Fork;
        if (_advanceRunUntil > 0f)
        {
            if (now < _advanceRunUntil)
            {
                SprintWhileInvestigating = true;
                decision = ECombatDecision.Search;
                return true;
            }
            // The run is over: hold and watch wherever the bot is.
            _advanceRunUntil = 0f;
            _ambushUntil = now + Random.Range(AMBUSH_TIME_MIN, AMBUSH_TIME_MAX);
            LogEvent("advance: the run is over, holding and watching");
            decision = ECombatDecision.SeekCover;
            return true;
        }
        if (inCover && !_advanceRolled && advanceFork.ShotAdvanceNoCoverChance > 0f && _vaguePositionTime > 0f && (Bot.Position - area).magnitude > Mathf.Max(INVESTIGATE_STOP_DISTANCE, _vagueUncertainty * 0.5f))
        {
            // No closer cover but still far from the area: an aggressive bot runs toward it instead of sitting (once per investigation).
            _advanceRolled = true;
            float advanceChance = advanceFork.ShotAdvanceNoCoverChance;
            advanceChance *= Mathf.Lerp(1f, GlobalSettings.Fork.ShotUncertaintyInvestigateMin, GlobalSettings.Fork.ShotUncertaintyHigh > 0f ? Mathf.InverseLerp(0f, GlobalSettings.Fork.ShotUncertaintyHigh, _vagueUncertainty) : 0f);
            switch (Bot.Info.Personality)
            {
                case EPersonality.Wreckless:
                case EPersonality.GigaChad:
                case EPersonality.Chad:
                    advanceChance = Mathf.Min(1f, advanceChance * advanceFork.ShotAggressivePull);
                    break;
                case EPersonality.Rat:
                case EPersonality.Timmy:
                case EPersonality.Coward:
                    advanceChance /= advanceFork.ShotTimidPull;
                    break;
            }
            if (Random.value < advanceChance)
            {
                _advanceRunUntil = now + advanceFork.ShotAdvanceNoCoverTime;
                LogEvent($"advance: no closer cover, {(Bot.Position - area).magnitude:0} m from the shot area, running toward it for up to {advanceFork.ShotAdvanceNoCoverTime:0} s (chance {advanceChance:0.00})");
                SprintWhileInvestigating = true;
                decision = ECombatDecision.Search;
                return true;
            }
            LogEvent($"advance: no closer cover, {(Bot.Position - area).magnitude:0} m from the shot area, holding (chance {advanceChance:0.00})");
        }
        if (inCover && TryStartNade(enemy, now, GlobalSettings.Fork.ShotGrenadeChanceAmbush, "ambush"))
        {
            decision = ECombatDecision.ThrowGrenade;
            return true;
        }
        if (inCover)
        {
            // Nothing closer to hop to: hold and watch (ambush), then the state ends.
            _ambushUntil = now + Random.Range(AMBUSH_TIME_MIN, AMBUSH_TIME_MAX);
            LogEvent("investigate: no cover closer to the shot area, holding cover and watching");
            decision = ECombatDecision.SeekCover;
            return true;
        }

        // In the open with no cover to hop to: last resort, SAIN's own search at a run.
        SprintWhileInvestigating = true;
        decision = ECombatDecision.Search;
        return true;
    }

    /// <summary>
    /// Getting shot at again while hunting is a reason to run: the bot goes back to cover and reassesses (Cover reaction, then a new follow-up pick).
    /// A fight is never interrupted by this.
    /// </summary>
    private bool TryBreakInvestigate(Enemy enemy, float distance, bool hit, float now)
    {
        if (Outcome != EShotReaction.Hunt || now < _hideUntil || Relocating || now < _nextBreakAllowed || now < _nextBreakRoll || IsCloseRange(distance))
        {
            return false;
        }
        float chance = hit ? INVESTIGATE_BREAK_HIT_CHANCE : INVESTIGATE_BREAK_BULLET_CHANCE;
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                chance /= GlobalSettings.Fork.ShotAggressivePull;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                chance = Mathf.Min(1f, chance * GlobalSettings.Fork.ShotTimidPull);
                break;
        }
        // One attempt per second: a burst of flyby bullets must not roll the chance again and again.
        _nextBreakRoll = now + 1f;
        if (Random.value > chance)
        {
            return false;
        }
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        Bot.Cover.StopAdvance();
        _nextBreakAllowed = now + BREAK_LOCKOUT;
        LogEvent($"shot at again while investigating ({(hit ? "hit" : "bullet")} at {distance:0} m): running for cover to reassess");
        Apply(enemy, EShotReaction.Cover, distance / reach, now, GlobalSettings.Fork);
        return true;
    }

    private bool IsCloseRange(float distance)
    {
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        float limit = Mathf.Clamp(reach * CLOSE_RANGE_REACH_FRACTION, CLOSE_RANGE_MIN, CLOSE_RANGE_MAX);
        return distance <= limit;
    }

    private void TrySelfPreservation(bool hit, float now, float distance)
    {
        // A fight is never interrupted: only the sheltering states move the bot out of cover that is being shot at.
        if (Outcome == EShotReaction.Fight || Relocating || _vaguePositionTime <= 0f || Bot.Cover.CoverInUse == null || now < _nextSelfPreserve)
        {
            return;
        }
        // A bush stops nothing even at point blank: a bot in one still leaves it when shot at (Combat Cover: Leave Bush At Close Range).
        if (IsCloseRange(distance) && !(GlobalSettings.Fork.CombatBushLeaveClose && Bot.Cover.InFoliageCover))
        {
            // Point blank: hopping from cover to cover would mean never shooting back. The bot holds and fires (see AllowsSuppress).
            if (now >= _nextCloseLog)
            {
                _nextCloseLog = now + 3f;
                LogEvent($"self preservation skipped: shooter is close ({distance:0} m), holding cover and firing back");
            }
            return;
        }
        // A bush stops nothing: shot at in foliage the bot reacts to a bullet as strongly as to a hit.
        // Close bullets stack: each one in the window adds a step to the bullet chance, up to a ceiling (suppression alone must not keep a bot moving).
        ForkSettings pressure = GlobalSettings.Fork;
        float bulletChance = Mathf.Min(Mathf.Max(SELF_PRESERVE_BULLET_CHANCE, pressure.ShotSelfPreserveBulletCeiling), SELF_PRESERVE_BULLET_CHANCE + (pressure.ShotSelfPreserveBulletStep * Mathf.Max(0, _bulletStack - 1)));
        float chance = hit || Bot.Cover.InFoliageCover ? SELF_PRESERVE_HIT_CHANCE : bulletChance;
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                chance /= GlobalSettings.Fork.ShotAggressivePull;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                chance = Mathf.Min(1f, chance * GlobalSettings.Fork.ShotTimidPull);
                break;
        }
        // Every attempt uses up the interval, hit or miss: a burst of flyby bullets must not roll the chance again and again.
        _nextSelfPreserve = now + SELF_PRESERVE_INTERVAL;
        if (Random.value > chance)
        {
            return;
        }
        bool inFoliage = Bot.Cover.InFoliageCover;
        if (Bot.Cover.RelocateFromFire(_vaguePosition))
        {
            LogEvent($"self preservation: shot at in {(inFoliage ? "foliage" : "cover")}, moving to another cover (chance {chance:0.00}, {_bulletStack} close bullets)");
            _bulletStack = 0;
        }
    }

    private float HopHoldTime()
    {
        float time = Random.Range(HOP_HOLD_MIN, HOP_HOLD_MAX);
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                return time / GlobalSettings.Fork.ShotAggressivePull;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                return time * GlobalSettings.Fork.ShotTimidPull;
        }
        return time;
    }

    /// <summary>Weighted choice of what the bot does once its hide is over.</summary>
    private EFollowUp ChooseFollowUp(Enemy enemy)
    {
        ForkSettings fork = GlobalSettings.Fork;
        bool farFight = _farFight;
        _farFight = false;
        float retreat = fork.ShotRelocateEnabled ? fork.ShotFollowRetreat : 0f;
        float fightOn = fork.ShotFollowFightOn;
        float investigate = fork.ShotFollowInvestigate;

        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                retreat /= fork.ShotAggressivePull;
                fightOn *= fork.ShotAggressivePull;
                investigate *= fork.ShotAggressivePull;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                retreat *= fork.ShotTimidPull;
                fightOn /= fork.ShotTimidPull;
                investigate /= fork.ShotTimidPull;
                break;
        }
        // Not sure where the shot came from: less going out to look for the shooter.
        float followUncertainty = FreshUncertainty;
        if (fork.ShotUncertaintyHigh > 0f && followUncertainty > 0f)
        {
            investigate *= Mathf.Lerp(1f, fork.ShotUncertaintyInvestigateMin, Mathf.InverseLerp(0f, fork.ShotUncertaintyHigh, followUncertainty));
        }
        ETagStatus health = Bot.Memory.Health.HealthStatus;
        if (health == ETagStatus.BadlyInjured || health == ETagStatus.Dying)
        {
            retreat *= INJURED_COVER_BONUS;
            investigate *= INJURED_FIGHT_PENALTY;
        }
        // Out of the weapon's reach there is nothing to fight with from cover.
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        if (enemy != null && enemy.RealDistance > reach * FIGHT_REACH_LIMIT)
        {
            fightOn *= 0.3f;
        }

        if (fork.ShotSquadRolesEnabled && fork.ShotSquadRoleStrength > 0f && Bot.Squad?.Members != null)
        {
            // A mate that fights makes this bot flank (investigate), a mate that hunts makes it cover fire (fight on). Capped at +100%.
            int fighting = 0;
            int hunting = 0;
            float rangeSqr = fork.ShotSquadRoleRange * fork.ShotSquadRoleRange;
            foreach (var member in Bot.Squad.Members.Values)
            {
                if (member == null || member == Bot || member.BotOwner == null || member.BotOwner.IsDead || member.ShotReaction == null)
                {
                    continue;
                }
                if ((member.Position - Bot.Position).sqrMagnitude > rangeSqr)
                {
                    continue;
                }
                EShotReaction mateState = member.ShotReaction.Outcome;
                if (mateState == EShotReaction.Fight || mateState == EShotReaction.FightOn)
                {
                    fighting++;
                }
                else if (mateState == EShotReaction.Hunt)
                {
                    hunting++;
                }
            }
            if (fighting > 0)
            {
                investigate *= 1f + Mathf.Min(1f, fork.ShotSquadRoleStrength * fighting);
            }
            if (hunting > 0)
            {
                fightOn *= 1f + Mathf.Min(1f, fork.ShotSquadRoleStrength * hunting);
            }
            if (fighting > 0 || hunting > 0)
            {
                LogEvent($"squad roles: {fighting} mate(s) fighting, {hunting} hunting");
            }
        }

        if (farFight)
        {
            // The roll said "fight": the reach, weapon and personality that decided it carry over as a stronger Fight On.
            fightOn *= fork.ShotFarFightOnBoost;
        }

        float total = retreat + fightOn + investigate;
        EFollowUp choice = EFollowUp.FightOn;
        if (total > 0f)
        {
            float pick = Random.value * total;
            if (pick < retreat)
            {
                choice = EFollowUp.Retreat;
            }
            else if (pick < retreat + investigate)
            {
                choice = EFollowUp.Investigate;
            }
        }
        LogEvent($"after hiding: {choice} (weights retreat {retreat:0.00}, fight on {fightOn:0.00}, investigate {investigate:0.00}{(farFight ? ", far fight boost" : "")})");
        return choice;
    }

    /// <summary>
    /// A Fight roll against an unseen shooter is "search for the one shooting at me": the bot faces the vague area, fires at it and works toward it
    /// (dogfight movement) for a few seconds. Without this, SAIN's own combat (which has little to do against an enemy it cannot see) walks around until it spots the shooter.
    /// </summary>
    private void StartFightSearch(float now, ForkSettings fork)
    {
        _searchRangeLogged = false;
        if (!fork.ShotFightSearchEnabled || fork.ShotFightSearchTime <= 0f)
        {
            _searchUntil = 0f;
            return;
        }
        _searchUntil = now + fork.ShotFightSearchTime;
        _searchFireUntil = now + Mathf.Min(fork.ShotFightFireTime, fork.ShotFightSearchTime);
        LogEvent($"fight: searching the vague area for {fork.ShotFightSearchTime:0.#} s, firing for {Mathf.Min(fork.ShotFightFireTime, fork.ShotFightSearchTime):0.#} s ({(fork.ShotFightSearchMove ? "moving" : "standing")})");
    }

    /// <summary>Called every steering tick by ShotSearchAction: look at the vague area and fire at it while the fire window lasts.</summary>
    public void FightSearchTick(Enemy enemy)
    {
        if (enemy == null || _vaguePositionTime <= 0f)
        {
            return;
        }
        Vector3 area = _vaguePosition;
        Bot.Steering.LookToFloorPoint(area);
        if (Time.time >= _searchFireUntil)
        {
            Bot.Suppression.ResetSuppressing();
            return;
        }
        float searchMax = GlobalSettings.Fork.ShotFightFireMaxUncertainty;
        if (searchMax > 0f && FreshUncertainty > searchMax)
        {
            // Too unsure where the shooter is: it watches the area without firing.
            Bot.Suppression.ResetSuppressing();
            return;
        }
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        if ((Bot.Position - area).magnitude > reach * FIGHT_REACH_LIMIT)
        {
            if (!_searchRangeLogged)
            {
                _searchRangeLogged = true;
                LogEvent("fight: the vague area is beyond the weapon reach, watching it without firing");
            }
            return;
        }
        Vector3 aim = area + Vector3.up * 1.2f;
        LogBlindFire("Fight", Bot.Suppression.SuppressPosition(aim, enemy), aim);
    }

    public void FightSearchEnded()
    {
        _blindFiring = false;
        Bot.Suppression.ResetSuppressing();
    }

    private bool _blindFiring;
    private float _nextBlindLog;

    /// <summary>
    /// Log line for the blind fire (Fight search, Fight On cycle): one line when it starts firing, and at most one per second saying why it is not.
    /// </summary>
    private void LogBlindFire(string mode, bool fired, Vector3 aim)
    {
        if (!GlobalSettings.Fork.ShotReactionLog)
        {
            return;
        }
        float now = Time.time;
        if (fired)
        {
            if (!_blindFiring)
            {
                _blindFiring = true;
                LogEvent($"blind fire ({mode}): firing at the vague area, {(Bot.Position - aim).magnitude:0} m, aim angle {Bot.Steering.AngleToPointFromLookDir(aim):0} deg");
            }
            return;
        }
        _blindFiring = false;
        if (now < _nextBlindLog)
        {
            return;
        }
        _nextBlindLog = now + 1f;
        var weapon = Bot.BotOwner.WeaponManager;
        string why;
        if (weapon.IsMelee)
        {
            why = "melee weapon in hand";
        }
        else if (!weapon.IsWeaponReady)
        {
            why = "weapon not ready";
        }
        else if (weapon.Reload.Reloading)
        {
            why = "reloading";
        }
        else if (!weapon.HaveBullets)
        {
            why = "no bullets";
        }
        else if (!Bot.BotOwner.ShootData.CanShootByState)
        {
            why = "cannot shoot in its current state (sprinting, changing weapon)";
        }
        else
        {
            float angle = Bot.Steering.AngleToPointFromLookDir(aim);
            why = angle > 10f ? $"not facing the area yet ({angle:0} deg, needs 10 or less)" : "friendly in the line of fire, or the shot was refused";
        }
        LogEvent($"blind fire ({mode}): not firing: {why}");
    }

    /// <summary>
    /// Fight On (holding cover and suppressing): fire bursts at the vague area on a fire / pause cycle instead of relying on SAIN's own suppression
    /// (which needs a known position it can trace and stays silent otherwise). True = the bot is firing now, the caller skips SAIN's suppression.
    /// </summary>
    public bool TryFightOnFire(Enemy enemy)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.ShotFightSearchEnabled || fork.ShotFightOnFirePause <= 0f || fork.ShotFightFireTime <= 0f)
        {
            return false;
        }
        if (!IsManaging(enemy) || Outcome != EShotReaction.FightOn || !_followDone || _vaguePositionTime <= 0f)
        {
            return false;
        }
        if (SeesShooter(enemy) && enemy.CanShoot)
        {
            return false;
        }
        float now = Time.time;
        if (_fireCycleStart < 0f)
        {
            _fireCycleStart = now;
        }
        // The less sure the bot is where the shooter is, the less it fires: nothing above the limit, shorter bursts and longer pauses below it.
        float fireUncertainty = FreshUncertainty;
        if (fork.ShotFightFireMaxUncertainty > 0f && fireUncertainty > fork.ShotFightFireMaxUncertainty)
        {
            _blindFiring = false;
            Bot.Suppression.ResetSuppressing();
            return false;
        }
        float fireT = fork.ShotUncertaintyHigh > 0f ? Mathf.InverseLerp(0f, fork.ShotUncertaintyHigh, fireUncertainty) : 0f;
        float fireTime = fork.ShotFightFireTime * Mathf.Lerp(1f, fork.ShotUncertaintyFireTimeMin, fireT);
        float cycle = fireTime + fork.ShotFightOnFirePause * Mathf.Lerp(1f, fork.ShotUncertaintyPauseMax, fireT);
        float phase = (now - _fireCycleStart) % cycle;
        if (phase >= fireTime)
        {
            _blindFiring = false;
            Bot.Suppression.ResetSuppressing();
            return false;
        }
        Vector3 area = _vaguePosition;
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        if ((Bot.Position - area).magnitude > reach * FIGHT_REACH_LIMIT)
        {
            return false;
        }
        Vector3 aim = area + Vector3.up * 1.2f;
        bool fired = Bot.Suppression.SuppressPosition(aim, enemy);
        LogBlindFire("Fight On", fired, aim);
        return fired;
    }

    private bool ApplyFollowUp(EFollowUp follow, float now, out ECombatDecision decision)
    {
        decision = ECombatDecision.None;
        if (follow == EFollowUp.Retreat && TryStartRelocating())
        {
            decision = ECombatDecision.Relocate;
            return true;
        }
        if (follow == EFollowUp.Investigate)
        {
            StartInvestigate(now);
        }
        else
        {
            StartFightOn(now);
        }
        return false;
    }

    private float NadeChance(EFollowUp follow)
    {
        ForkSettings fork = GlobalSettings.Fork;
        switch (follow)
        {
            case EFollowUp.Retreat:
                return fork.ShotGrenadeChanceRetreat;
            case EFollowUp.Investigate:
                return fork.ShotGrenadeChanceInvestigate;
            default:
                return fork.ShotGrenadeChanceFightOn;
        }
    }

    /// <summary>
    /// A grenade at the vague shot area, thrown from cover (SAIN's own throw logic decides the target and the safety checks). True = the throw has started;
    /// the caller returns the ThrowGrenade decision and the continuation runs from TryGetDecision.
    /// </summary>
    private bool TryStartNade(Enemy enemy, float now, float chance, string why)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.ShotGrenadeEnabled || chance <= 0f || now < _nextNadeTime || _vaguePositionTime <= 0f)
        {
            return false;
        }
        var grenades = Bot.BotOwner.WeaponManager?.Grenades;
        if (grenades == null || !grenades.HaveGrenade)
        {
            return false;
        }
        float areaDistance = (Bot.Position - _vaguePosition).magnitude;
        if (areaDistance < fork.ShotGrenadeMinDistance || areaDistance > fork.ShotGrenadeMaxDistance)
        {
            return false;
        }
        // A grenade's splash is a few meters wide: no throw at an area that is too uncertain, fewer throws the vaguer it is.
        float nadeUncertainty = FreshUncertainty;
        if (fork.ShotNadeMaxUncertainty > 0f && nadeUncertainty > fork.ShotNadeMaxUncertainty)
        {
            return false;
        }
        chance *= Mathf.Lerp(1f, 0.3f, fork.ShotUncertaintyHigh > 0f ? Mathf.InverseLerp(0f, fork.ShotUncertaintyHigh, nadeUncertainty) : 0f);
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                chance = Mathf.Min(1f, chance * fork.ShotAggressivePull);
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                chance /= fork.ShotTimidPull;
                break;
        }
        if (Random.value > chance)
        {
            return false;
        }
        if (!Bot.Grenade.GrenadeThrowDecider.GetDecision(enemy, out string reason))
        {
            LogEvent($"grenade: not thrown ({why}): {reason}");
            return false;
        }
        _nextNadeTime = now + fork.ShotGrenadeCooldown;
        _nadeActive = true;
        _nadeStart = now;
        _nadeUntil = now + 3f;
        LogEvent($"grenade: throwing at the vague area, {areaDistance:0} m ({why})");
        return true;
    }

    /// <summary>Stay in cover and fight back: normal combat against the vague area (suppressive fire allowed), or against the shooter if it is seen.</summary>
    private void StartFightOn(float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        Outcome = EShotReaction.FightOn;
        _hideUntil = now;
        _expireTime = now + fork.ShotFightOnDuration;
    }

    /// <summary>
    /// The retreat is over and the bot has not seen the shooter: it takes the nearest cover and holds it, then investigates cover to cover
    /// (the same Investigate state), so its way back is never SAIN's plain upright search walk.
    /// </summary>
    private void StartPostRetreat(Enemy enemy, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float hold = Random.Range(POST_RETREAT_HOLD_MIN, POST_RETREAT_HOLD_MAX);
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                hold /= fork.ShotAggressivePull;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                hold *= fork.ShotTimidPull;
                break;
        }
        _enemy = enemy;
        Outcome = EShotReaction.Hunt;
        _followDone = true;
        _noCoverDone = true;
        // Hold until it feels safe (random threshold, timid ones want more, aggressive ones less); the old hold time is the upper limit.
        float threshold = Random.Range(Mathf.Min(fork.ShotSafetyThresholdMin, fork.ShotSafetyThresholdMax), Mathf.Max(fork.ShotSafetyThresholdMin, fork.ShotSafetyThresholdMax));
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                threshold -= 0.1f;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                threshold += 0.05f;
                break;
        }
        _safetyThreshold = Mathf.Clamp(threshold, 0.5f, 0.95f);
        _postRetreat = true;
        float minHold = POST_RETREAT_MIN_HOLD;
        if (_longRetreat)
        {
            // After a duel or pressure retreat the bot stays away for at least this long, whatever its safety says.
            _longRetreat = false;
            minHold = Mathf.Max(minHold, fork.ShotLongRetreatMinHold);
            if (_hurtLeave)
            {
                // Badly hurt: long enough in cover for SAIN's own healing to run before it investigates again.
                _hurtLeave = false;
                minHold = Mathf.Max(minHold, fork.ShotHurtMinHold);
            }
            hold = Mathf.Max(hold, minHold);
        }
        _holdMinUntil = now + minHold;
        _nextMobileHop = now + Random.Range(5f, 9f);
        _safetyCacheTime = -999f;
        _hideUntil = now + hold;
        _investigateStart = _hideUntil;
        _ambushUntil = 0f;
        _hopHold = HopHoldTime();
        _inCoverSince = -1f;
        _expireTime = _hideUntil + fork.ShotHuntTimeMax;
        Bot.Cover.StartShotCover(_vaguePosition, false);
        LogEvent($"retreat over: holding cover until it feels safe (safety >= {_safetyThreshold:0.00}, at most {hold:0} s), then investigating cover to cover");
    }

    /// <summary>
    /// A Fight or Fight On ran out and the bot never saw the shooter: it does not fall back to plain SAIN (a stroll, or forgetting) but goes on
    /// as an investigation (cover to cover toward the vague area, ambush, then the sprinting search). Every path of the reaction ends in one.
    /// </summary>
    private bool ChainInvestigation(float now)
    {
        if ((Outcome != EShotReaction.Fight && Outcome != EShotReaction.FightOn) || _enemy == null)
        {
            return false;
        }
        LogEvent($"{Outcome} over, still no sight of the shooter: investigating cover to cover");
        _followDone = true;
        _noCoverDone = true;
        StartInvestigate(now);
        return true;
    }

    /// <summary>Go looking for the shooter: the normal search toward the vague area, until the shooter is seen or the time runs out.</summary>
    private void StartInvestigate(float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        Outcome = EShotReaction.Hunt;
        _hideUntil = now;
        _investigateStart = now;
        _ambushUntil = 0f;
        _advanceRunUntil = 0f;
        _advanceRolled = false;
        _hopHold = HopHoldTime();
        _expireTime = now + fork.ShotHuntTimeMax;
    }

    /// <summary>
    /// Picks a point on the far side from the vague shot area and starts walking there. False = no usable point, the bot goes back to its normal behavior.
    /// </summary>
    private bool _quietRelocate;

    /// <summary>
    /// A retreat at a scaled distance (duel, pressure) may find no navmesh point that far (a roof, a compound, the edge of the map). It then falls back to shorter distances
    /// (60%, 35% and 20% of it, never under 15 m) before it gives up, so a bot that wants to leave does not just stay.
    /// </summary>
    private bool TryStartRelocating(float? distanceOverride = null)
    {
        bool log = GlobalSettings.Fork.ShotReactionLog;
        _quietRelocate = distanceOverride.HasValue;
        bool started = TryStartRelocatingAt(distanceOverride);
        if (!started && distanceOverride.HasValue)
        {
            foreach (float share in RELOCATE_FALLBACK_SHARES)
            {
                float shorter = distanceOverride.Value * share;
                if (shorter < 15f)
                {
                    break;
                }
                if (TryStartRelocatingAt(shorter))
                {
                    started = true;
                    if (log)
                    {
                        LogEvent($"retreat: no point at {distanceOverride.Value:0} m, fell back to {shorter:0} m");
                    }
                    break;
                }
            }
        }
        _quietRelocate = false;
        if (!started && distanceOverride.HasValue && log)
        {
            Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Tag}: no relocation point found at {distanceOverride.Value:0} m or any shorter distance, back to normal");
        }
        return started;
    }

    private static readonly float[] RELOCATE_FALLBACK_SHARES = [0.6f, 0.35f, 0.2f];

    private bool TryStartRelocatingAt(float? distanceOverride)
    {
        long started = ShotPerf.Start();
        bool result = TryStartRelocatingAtCore(distanceOverride);
        ShotPerf.Stop(ShotPerf.Relocate, started);
        return result;
    }

    private bool TryStartRelocatingAtCore(float? distanceOverride)
    {
        // A pressure retreat of a Rogue or Goon goes down (off the roof) when it can; any other point is the fallback.
        if (_retreatDown)
        {
            _requireLower = true;
            bool lower = RelocatePasses(distanceOverride);
            _requireLower = false;
            if (lower)
            {
                return true;
            }
        }
        // A point out of the shooter's line of sight is preferred (the bot is gone, not just further away); any point is the fallback.
        return RelocatePasses(distanceOverride);
    }

    private bool _requireHidden;

    /// <summary>
    /// Set while a bot that SEES the player retreats (duel, hurt, squad kill): the retreat is planned against the player's real position, not the vague ring around it, so "out of sight"
    /// and "further away" mean what they say. Null = the usual shot area (an unseen shooter).
    /// </summary>
    private Vector3? _knownThreat;
    private float _lastDuelHitTime = -999f;

    /// <summary>
    /// The bot knows where the player is: it sees him now, or saw him within ShotKnownPositionWindow seconds (then the position is where it last saw him). Otherwise it only has the rough area.
    /// A bot that knows plans its retreat against that position and only goes somewhere out of the player's sight; one that does not keeps the rough-area behavior.
    /// </summary>
    private bool KnowsPosition(Enemy enemy, out Vector3 position)
    {
        position = Vector3.zero;
        if (enemy == null)
        {
            return false;
        }
        if (enemy.IsVisible)
        {
            position = enemy.EnemyPosition;
            return true;
        }
        if (enemy.Seen && enemy.TimeSinceSeen <= GlobalSettings.Fork.ShotKnownPositionWindow && enemy.LastKnownPosition != null)
        {
            position = enemy.LastKnownPosition.Value;
            return true;
        }
        return false;
    }

    /// <summary>
    /// The bot is engaged in actual combat with the player: it fired at him in the last 8 s, or he hit it or put a bullet close to it in the last 10 s. Merely being able to see him is not enough.
    /// Only an engaged bot plans its break-off against the player's real position and rolls the hurt and squad-death break-offs.
    /// </summary>
    private bool IsEngaged(float now)
    {
        ShootData shootData = Bot.BotOwner?.ShootData;
        return (shootData != null && now - shootData.LastTriggerPressd < 8f) || now - _lastDuelHitTime < 10f;
    }

    /// <summary>A point out of the shooter's sight first, then (unless the retreat needs one, see _requireHidden) any point.</summary>
    private bool RelocatePasses(float? distanceOverride)
    {
        return TryRelocatePass(distanceOverride, true) || (!_requireHidden && TryRelocatePass(distanceOverride, false));
    }

    private bool BreaksLineOfSight(Vector3 shotArea, Vector3 point)
    {
        return SAINCoverClass.BlocksSight(shotArea, point);
    }

    // ---- "how sure am I that nobody is after me" ----

    /// <summary>Safety (0..1): quiet time, distance from the shot area and being out of its sight. Shots and sightings reset the quiet time.</summary>
    private static float SAFETY_QUIET_FULL => GlobalSettings.Fork.ShotSafetyQuietTime;
    private static float SAFETY_DIST_START => GlobalSettings.Fork.ShotSafetyDistStart;
    private static float SAFETY_DIST_FULL => GlobalSettings.Fork.ShotSafetyDistFull;
    private static float POST_RETREAT_MIN_HOLD => GlobalSettings.Fork.ShotPostRetreatMinHold;
    private static float MOBILE_SAFETY => GlobalSettings.Fork.ShotMobileSafety;
    private static float REPEAT_RELOCATE_INTERVAL => GlobalSettings.Fork.ShotRepeatInterval;
    private static int REPEAT_RELOCATE_MAX => GlobalSettings.Fork.ShotRepeatMax;

    /// <summary>After a hunt runs out (ambush over, time up, expired) with the shooter still unseen, SAIN's own search takes over: for this long it sprints instead of strolling.</summary>
    private static float RETURN_WINDOW => GlobalSettings.Fork.ShotReturnWindow;

    private float _returnUntil;
    private Enemy _returnEnemy;

    /// <summary>True while a bot whose hunt just ran out searches for this enemy at a run (set up by End, read by the search action).</summary>
    public bool IsReturning(Enemy enemy)
    {
        return enemy != null && enemy == _returnEnemy && Time.time < _returnUntil;
    }

    private float _lastThreatTime = -999f;
    private float _safetyCache;
    private float _safetyCacheTime = -999f;
    private float _safetyBeforeShot;
    private bool _postRetreat;
    private float _holdMinUntil;
    private float _safetyThreshold;
    private float _nextMobileHop;
    private float _nextRepeatRelocate;
    private float _nextBreakRoll;
    private float _hopBestDistance = float.MaxValue;
    private float _hopProgressTime;
    private float _retreatStarted = -999f;
    private int _repeatRelocations;

    private float Safety(float now, bool fresh = false)
    {
        if (!fresh && now - _safetyCacheTime < 1f)
        {
            return _safetyCache;
        }
        Vector3 area = now - _vaguePositionTime < 600f ? _vaguePosition : Bot.Position;
        float quiet = Mathf.Clamp01((now - Mathf.Max(_lastThreatTime, _lastSeenTime)) / SAFETY_QUIET_FULL);
        float far = Mathf.Clamp01(((Bot.Position - area).magnitude - SAFETY_DIST_START) / (SAFETY_DIST_FULL - SAFETY_DIST_START));
        float sight = SAINCoverClass.BlocksSight(area, Bot.Position) ? 1f : 0f;
        _safetyCache = (0.45f * quiet) + (0.35f * far) + (0.2f * sight);
        _safetyCacheTime = now;
        return _safetyCache;
    }

    /// <summary>
    /// After a retreat the bot holds until it feels safe (a random threshold, personality shifted), at least a short while and at most the old hold time.
    /// While it does not feel safe it does not sit still: it moves to another cover every few seconds.
    /// </summary>
    private void UpdatePostRetreatHold(float now)
    {
        if (now >= _hideUntil)
        {
            // The longest hold is over: from here it is the normal investigation.
            _postRetreat = false;
            return;
        }
        float safety = Safety(now);
        if (now >= _holdMinUntil && safety >= _safetyThreshold)
        {
            _postRetreat = false;
            _hideUntil = now;
            _investigateStart = now;
            _expireTime = now + GlobalSettings.Fork.ShotHuntTimeMax;
            LogEvent($"safe enough (safety {safety:0.00} >= {_safetyThreshold:0.00}): ending the hold, investigating cover to cover");
            return;
        }
        if (safety < MOBILE_SAFETY && now >= _nextMobileHop && Bot.Cover.CoverInUse != null)
        {
            _nextMobileHop = now + Random.Range(GlobalSettings.Fork.ShotMobileHopMin, GlobalSettings.Fork.ShotMobileHopMax);
            if (Bot.Cover.RelocateFromFire(_vaguePosition))
            {
                LogEvent($"not sure it is safe (safety {safety:0.00}): moving to another cover");
            }
        }
    }

    /// <summary>
    /// Shot at again while retreating or holding after a retreat: a chance of another retreat (a fresh route), higher if the bot had just felt safe
    /// (startled). At most twice per reaction chain, and never against a close shooter (that is a fight).
    /// </summary>
    private bool TryRepeatRelocation(bool hit, float distance, float now)
    {
        if ((!Relocating && !_postRetreat) || now < _nextRepeatRelocate || now - _retreatStarted < GlobalSettings.Fork.ShotRepeatMinRetreatTime || _repeatRelocations >= REPEAT_RELOCATE_MAX || IsCloseRange(distance))
        {
            return false;
        }
        ForkSettings fork = GlobalSettings.Fork;
        bool startled = _safetyBeforeShot >= fork.ShotRepeatStartledSafety;
        float chance = hit ? fork.ShotRepeatHitChance : fork.ShotRepeatBulletChance;
        if (startled)
        {
            chance = Mathf.Min(1f, chance + fork.ShotRepeatStartledBonus);
        }
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                chance /= GlobalSettings.Fork.ShotAggressivePull;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                chance = Mathf.Min(1f, chance * GlobalSettings.Fork.ShotTimidPull);
                break;
        }
        // Every attempt uses up the interval (a burst must not roll again and again).
        _nextRepeatRelocate = now + REPEAT_RELOCATE_INTERVAL;
        if (Random.value > chance)
        {
            return false;
        }
        float max = Mathf.Max(GlobalSettings.Fork.ShotRelocateMinDistance, GlobalSettings.Fork.ShotRelocateMaxDistance);
        float retreat = startled ? max * 1.25f : max;
        if (!TryStartRelocating(retreat))
        {
            return false;
        }
        _repeatRelocations++;
        LogEvent($"shot again ({(hit ? "hit" : "bullet")} at {distance:0} m, safety was {_safetyBeforeShot:0.00}{(startled ? ", startled" : "")}): another retreat, {retreat:0} m");
        return true;
    }

    // ---- retreat route and destination ----

    private const float WAYPOINT_SWITCH_DISTANCE = 6f;
    private const float WAYPOINT_TIMEOUT = 14f;
    private const float DESTINATION_COVER_RADIUS = 4f;

    private enum ERoute
    {
        Direct,
        Bend,
        ZigZag,
        CoverFirst,
    }

    private readonly List<Vector3> _route = [];
    private int _routeIndex;
    private float _legStart;
    private readonly NavMeshPath _legPath = new();
    private readonly Collider[] _destColliders = new Collider[16];
    private int _destMask = -1;

    /// <summary>The point the bot is running to right now: the next waypoint of the retreat route, the destination on the last leg.</summary>
    public Vector3 CurrentWaypoint
    {
        get { return _routeIndex < _route.Count ? _route[_routeIndex] : RelocatePoint; }
    }

    /// <summary>Moves on to the next leg when the current waypoint is reached (or the leg takes too long). True = the leg changed.</summary>
    public bool UpdateRouteProgress()
    {
        if (!Relocating || _routeIndex >= _route.Count - 1)
        {
            return false;
        }
        bool reached = (Bot.Position - _route[_routeIndex]).sqrMagnitude < WAYPOINT_SWITCH_DISTANCE * WAYPOINT_SWITCH_DISTANCE;
        if (!reached && Time.time - _legStart < WAYPOINT_TIMEOUT)
        {
            return false;
        }
        _routeIndex++;
        _legStart = Time.time;
        return true;
    }

    /// <summary>How well a retreat destination hides the bot: solid cover next to it, or a bush that is far and out of sight. 0 = open ground.</summary>
    private float DestinationCoverQuality(Vector3 point, Vector3 shotArea)
    {
        if (_destMask < 0)
        {
            _destMask = (1 << LayerMask.NameToLayer("HighPolyCollider")) | (1 << LayerMask.NameToLayer("Foliage"));
        }
        int foliageLayer = LayerMask.NameToLayer("Foliage");
        bool bushBan = Bot.Cover.BushBanNow();
        int count = Physics.OverlapSphereNonAlloc(point, DESTINATION_COVER_RADIUS, _destColliders, _destMask);
        float best = 0f;
        for (int i = 0; i < count; i++)
        {
            Collider collider = _destColliders[i];
            if (collider == null)
            {
                continue;
            }
            if (bushBan && collider.gameObject.layer == foliageLayer)
            {
                continue;
            }
            Vector3 size = collider.bounds.size;
            if (size.y < 0.75f || (size.x < 0.25f && size.z < 0.25f) || size.x > 30f || size.y > 30f || size.z > 30f)
            {
                continue;
            }
            float quality = collider.gameObject.layer == foliageLayer ? SAINCoverClass.FoliageQuality(point, shotArea) : 1f;
            if (quality > best)
            {
                best = quality;
            }
        }
        return best * 1.5f;
    }

    /// <summary>The best score a destination's cover can add (DestinationCoverQuality returns at most 1 x 1.5).</summary>
    private const float MAX_DESTINATION_COVER = 1.5f;

    private readonly NavMeshPath _reachPath = new();
    private static readonly Vector3[] _reachCorners = new Vector3[2];

    /// <summary>Same test as the mover's CanGoToPoint (complete path from the bot, more than one corner) without a new path object and corner array per call.</summary>
    private bool CanReachFast(Vector3 point)
    {
        var navData = Bot.Transform.NavData;
        if (!navData.IsOnNavMesh || !NavMesh.SamplePosition(point, out NavMeshHit hit, 0.5f, -1))
        {
            return false;
        }
        return NavMesh.CalculatePath(navData.Position, hit.position, -1, _reachPath)
            && _reachPath.status == NavMeshPathStatus.PathComplete
            && _reachPath.GetCornersNonAlloc(_reachCorners) > 1;
    }

    private bool LegOk(Vector3 a, Vector3 b)
    {
        if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, _legPath) || _legPath.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }
        float length = 0f;
        Vector3[] corners = _legPath.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            length += Vector3.Distance(corners[i], corners[i + 1]);
        }
        return length <= (Vector3.Distance(a, b) * 1.6f) + 5f;
    }

    private static bool TryOffsetPoint(Vector3 from, Vector3 dest, float t, float offset, out Vector3 point)
    {
        Vector3 dir = dest - from;
        dir.y = 0f;
        dir.Normalize();
        Vector3 perpendicular = new(-dir.z, 0f, dir.x);
        Vector3 target = Vector3.Lerp(from, dest, t) + (perpendicular * offset);
        Vector3? sampled = NavMeshHelpers.GetNearbyNavMeshPoint(target, 4f);
        point = sampled ?? Vector3.zero;
        return sampled != null;
    }

    /// <summary>Share of the straight line to the destination that the shot area can see (0 = fully hidden, 1 = fully exposed).</summary>
    private float LineExposure(Vector3 from, Vector3 dest, Vector3 shotArea)
    {
        const int SAMPLES = 8;
        int visible = 0;
        for (int i = 1; i <= SAMPLES; i++)
        {
            Vector3 point = Vector3.Lerp(from, dest, i / (SAMPLES + 1f));
            if (!SAINCoverClass.BlocksSight(shotArea, point))
            {
                visible++;
            }
        }
        return visible / (float)SAMPLES;
    }

    private ERoute PickRoute(float exposure)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float direct = fork.ShotRouteDirect;
        float bend = fork.ShotRouteBend;
        float zigzag = fork.ShotRouteZigZag;
        float coverFirst = fork.ShotRouteCoverFirst;
        if (exposure > fork.ShotRouteExposureLimit)
        {
            // Mostly in the shooter's sight: the robotic straight run is the worst choice.
            direct = 5f;
            zigzag = fork.ShotRouteZigZag * 1.25f;
            coverFirst = fork.ShotRouteCoverFirst * 1.75f;
        }
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                direct *= 1.5f;
                bend *= 0.8f;
                zigzag *= 0.8f;
                coverFirst *= 0.8f;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                direct *= 0.6f;
                zigzag *= 1.3f;
                coverFirst *= 1.4f;
                break;
        }
        float pick = Random.value * (direct + bend + zigzag + coverFirst);
        if (pick < direct)
        {
            return ERoute.Direct;
        }
        if (pick < direct + bend)
        {
            return ERoute.Bend;
        }
        if (pick < direct + bend + zigzag)
        {
            return ERoute.ZigZag;
        }
        return ERoute.CoverFirst;
    }

    /// <summary>Plans the retreat as a route: sometimes straight, sometimes bent, zig-zagged or first out of sight. The last point is always the destination.</summary>
    private void BuildRoute(Vector3 from, Vector3 dest, Vector3 shotArea)
    {
        _route.Clear();
        _routeIndex = 0;
        _legStart = Time.time;
        ERoute variant = ERoute.Direct;
        float exposure = -1f;
        if ((dest - from).magnitude > 20f)
        {
            exposure = LineExposure(from, dest, shotArea);
            variant = PickRoute(exposure);
            if (variant != ERoute.Direct && !TryBuildVariant(variant, from, dest, shotArea))
            {
                _route.Clear();
                variant = ERoute.Bend;
                if (!TryBuildVariant(variant, from, dest, shotArea))
                {
                    _route.Clear();
                    variant = ERoute.Direct;
                }
            }
        }
        _route.Add(dest);
        LogEvent($"route: {variant}, {_route.Count - 1} waypoint(s), straight line {(exposure < 0f ? "short" : $"{exposure * 100f:0}% exposed")}");
    }

    private bool TryBuildVariant(ERoute variant, Vector3 from, Vector3 dest, Vector3 shotArea)
    {
        float side = Random.value < 0.5f ? -1f : 1f;
        switch (variant)
        {
            case ERoute.Bend:
            {
                if (!TryOffsetPoint(from, dest, Random.Range(0.35f, 0.65f), side * Random.Range(GlobalSettings.Fork.ShotRouteOffsetMin, GlobalSettings.Fork.ShotRouteOffsetMax), out Vector3 waypoint) || !LegOk(from, waypoint) || !LegOk(waypoint, dest))
                {
                    return false;
                }
                _route.Add(waypoint);
                return true;
            }
            case ERoute.ZigZag:
            {
                if (
                    !TryOffsetPoint(from, dest, Random.Range(0.25f, 0.4f), side * Random.Range(GlobalSettings.Fork.ShotRouteOffsetMin, GlobalSettings.Fork.ShotRouteOffsetMax), out Vector3 first)
                    || !TryOffsetPoint(from, dest, Random.Range(0.6f, 0.75f), -side * Random.Range(GlobalSettings.Fork.ShotRouteOffsetMin, GlobalSettings.Fork.ShotRouteOffsetMax), out Vector3 second)
                    || !LegOk(from, first)
                    || !LegOk(first, second)
                    || !LegOk(second, dest)
                )
                {
                    return false;
                }
                _route.Add(first);
                _route.Add(second);
                return true;
            }
            case ERoute.CoverFirst:
            {
                // The nearest point (10 to 26 m) that the shot area cannot see, then on to the destination.
                Vector3 away = from - shotArea;
                away.y = 0f;
                if (away.sqrMagnitude < 1f)
                {
                    return false;
                }
                away.Normalize();
                float[] radii = [10f, 18f, 26f];
                float[] angles = [0f, 40f, -40f, 80f, -80f];
                for (int r = 0; r < radii.Length; r++)
                {
                    int offset = Random.Range(0, angles.Length);
                    for (int a = 0; a < angles.Length; a++)
                    {
                        Vector3 direction = Quaternion.Euler(0f, angles[(a + offset) % angles.Length], 0f) * away;
                        Vector3? sampled = NavMeshHelpers.GetNearbyNavMeshPoint(from + (direction * radii[r]), 4f);
                        if (sampled == null || !SAINCoverClass.BlocksSight(shotArea, sampled.Value) || !LegOk(from, sampled.Value) || !LegOk(sampled.Value, dest))
                        {
                            continue;
                        }
                        _route.Add(sampled.Value);
                        return true;
                    }
                }
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// A retreat point near a squadmate scores higher: full bonus next to a living mate, nothing at the squad range.
    /// </summary>
    private float SquadBonus(Vector3 point)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (fork.ShotRetreatSquadWeight <= 0f || Bot.Squad?.Members == null)
        {
            return 0f;
        }
        float nearestSqr = float.MaxValue;
        foreach (var member in Bot.Squad.Members.Values)
        {
            if (member == null || member == Bot || member.BotOwner == null || member.BotOwner.IsDead)
            {
                continue;
            }
            float sqr = (member.Position - point).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
            }
        }
        if (nearestSqr == float.MaxValue)
        {
            return 0f;
        }
        float range = Mathf.Max(fork.ShotRetreatSquadRange, 1f);
        return fork.ShotRetreatSquadWeight * (1f - Mathf.Clamp01(Mathf.Sqrt(nearestSqr) / range));
    }

    private bool TryRelocatePass(float? distanceOverride, bool requireBrokenLos)
    {
        ForkSettings fork = GlobalSettings.Fork;
        Vector3 from = Bot.Position;
        Vector3 shotArea = _knownThreat ?? (Time.time - _vaguePositionTime < VAGUE_POSITION_MAX_AGE ? _vaguePosition : (_enemy != null ? _enemy.EnemyPosition : from));
        Vector3 away = from - shotArea;
        away.y = 0f;
        if (away.sqrMagnitude < 1f)
        {
            Vector2 random = Random.insideUnitCircle.normalized;
            away = new Vector3(random.x, 0f, random.y);
            away.y = 0f;
        }
        away.Normalize();

        float min = Mathf.Min(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
        float max = Mathf.Max(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
        float distance = distanceOverride ?? Random.Range(min, max);
        float currentToShot = (from - shotArea).magnitude;

        // Every valid point on the three rings is a candidate; the best one wins (hiding place next to it, a little randomness so retreats differ).
        bool found = false;
        float bestScore = float.MinValue;
        Vector3 best = Vector3.zero;
        for (int step = 0; step < 3; step++)
        {
            float stepDistance = distance * (1f - (step * 0.25f));
            for (int i = 0; i < RELOCATE_ANGLES.Length; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, RELOCATE_ANGLES[i], 0f) * away;
                long perf = ShotPerf.Start();
                Vector3? sampled = NavMeshHelpers.GetNearbyNavMeshPoint(from + (direction * stepDistance), 4f);
                ShotPerf.Stop(ShotPerf.RelNav, perf);
                if (sampled == null)
                {
                    continue;
                }
                // Must actually end up further from the shot area than the bot is now.
                if ((sampled.Value - shotArea).magnitude < currentToShot + (stepDistance * 0.5f))
                {
                    continue;
                }
                if (_requireLower && sampled.Value.y > from.y - fork.ShotRetreatDownMin)
                {
                    continue;
                }
                // Cheapest checks first; the expensive ones (collider scan, path calculations) only for a candidate that can still beat the best one so far.
                float cheap = ((2 - step) * 0.25f) + SquadBonus(sampled.Value) + Random.Range(0f, 0.6f)
                    + (_requireLower ? Mathf.Clamp01((from.y - sampled.Value.y) / 15f) * 0.5f : 0f);
                if (found && cheap + MAX_DESTINATION_COVER <= bestScore)
                {
                    continue;
                }
                if (requireBrokenLos)
                {
                    perf = ShotPerf.Start();
                    bool blocked = BreaksLineOfSight(shotArea, sampled.Value);
                    ShotPerf.Stop(ShotPerf.RelLos, perf);
                    if (!blocked)
                    {
                        continue;
                    }
                }
                perf = ShotPerf.Start();
                float score = cheap + DestinationCoverQuality(sampled.Value, shotArea);
                ShotPerf.Stop(ShotPerf.RelCover, perf);
                if (found && score <= bestScore)
                {
                    continue;
                }
                perf = ShotPerf.Start();
                bool pathOk = !(_avoidDistance > 0f && PathPassesNear(from, sampled.Value, _avoidPos, _avoidDistance)) && CanReachFast(sampled.Value);
                ShotPerf.Stop(ShotPerf.RelPath, perf);
                if (!pathOk)
                {
                    continue;
                }
                found = true;
                bestScore = score;
                best = sampled.Value;
            }
        }
        if (found)
        {
            RelocatePoint = best;
            Bot.Cover.ClearShotState();
            _postRetreat = false;
            Relocating = true;
            _stallRefTime = -1f;
            _retreatStarted = Time.time;
            _relocateEnd = Time.time + fork.ShotRelocateTimeMax;
            Outcome = EShotReaction.None;
            _hideUntil = 0f;
            _expireTime = 0f;
            if (fork.ShotReactionLog)
            {
                Logger.LogInfoAlways(
                    $"[ShotReaction] t={Time.time:0.0} {Tag}: relocating {(best - from).magnitude:0} m, {(best - shotArea).magnitude:0} m from the shot area"
                );
            }
            BuildRoute(from, best, shotArea);
            return true;
        }
        if (!requireBrokenLos && fork.ShotReactionLog && !_quietRelocate)
        {
            Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Tag}: no relocation point found, back to normal");
        }
        return false;
    }

    /// <summary>
    /// Called by the cover logic when the bot wants cover and has none. After a short grace it decides, once per roll: fight (normal combat) or flee
    /// (retreat out of the shooter's sight, farther the closer the shooter is). A bot that cannot find a place to flee to is cornered and fights.
    /// True = the reaction took over (the bot's state changed); false = keep the stopgap.
    /// </summary>
    public bool HandleNoCover(Enemy enemy)
    {
        if (!IsManaging(enemy) || Relocating || _followDone || _noCoverDone || Outcome == EShotReaction.Fight)
        {
            return false;
        }
        float now = Time.time;
        if (_noCoverSince < 0f || now - _lastNoCoverCall > 0.5f)
        {
            _noCoverSince = now;
        }
        _lastNoCoverCall = now;
        if (now - _noCoverSince < NO_COVER_GRACE)
        {
            return false;
        }
        _noCoverDone = true;

        ForkSettings fork = GlobalSettings.Fork;
        float distance = enemy.RealDistance;
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        float ratio = distance / reach;
        float score = NoCoverFightScore(distance, ratio, out string why);
        float fightChance = 1f / (1f + Mathf.Exp(-score));
        bool fight = Random.value < fightChance;
        if (!fight)
        {
            // Close shooter: get far. Distant shooter: just get out of sight (the short end of the range).
            float min = Mathf.Min(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
            float max = Mathf.Max(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
            float fleeDistance = Mathf.Lerp(max, min, Mathf.InverseLerp(0.5f, 1.5f, ratio));
            if (TryStartRelocating(fleeDistance))
            {
                LogEvent($"no cover: flight (fight chance {fightChance:0.00}: {why}), fleeing {fleeDistance:0} m");
                return true;
            }
            LogEvent("no cover: nowhere to flee to, cornered");
        }
        LogEvent($"no cover: fight (fight chance {fightChance:0.00}: {why})");
        Outcome = EShotReaction.Fight;
        _followDone = true;
        _hideUntil = now;
        _expireTime = now + fork.ShotFightDuration;
        StartFightSearch(now, fork);
        SprintWhileInvestigating = false;
        Bot.Cover.ClearShotState();
        return true;
    }

    /// <summary>
    /// Fork: the no-cover decision for a bot the reaction does not manage (it saw the player, or the reaction is over): the same fight-or-flight roll as HandleNoCover,
    /// planned against the player's real position when the bot knows it. True = a retreat started; false = fight (the caller stops seeking cover and the combat decision shoots).
    /// </summary>
    public bool HandleNoCoverAny(Enemy enemy)
    {
        if (!Enabled || enemy == null || Relocating)
        {
            return Relocating;
        }
        float now = Time.time;
        ForkSettings fork = GlobalSettings.Fork;
        float distance = enemy.RealDistance;
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        float ratio = distance / reach;
        float score = NoCoverFightScore(distance, ratio, out string why);
        float fightChance = 1f / (1f + Mathf.Exp(-score));
        if (Random.value < fightChance)
        {
            LogEvent($"no cover (not managed): fight (fight chance {fightChance:0.00}: {why})");
            return false;
        }
        float min = Mathf.Min(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
        float max = Mathf.Max(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
        float fleeDistance = Mathf.Lerp(max, min, Mathf.InverseLerp(0.5f, 1.5f, ratio));
        _enemy = enemy;
        _forcedRetreat = true;
        bool known = KnowsPosition(enemy, out Vector3 knownPosition);
        _requireHidden = known && fork.ShotKnownRetreatNeedsHidden;
        _knownThreat = known ? knownPosition : null;
        bool started = TryStartRelocating(fleeDistance);
        _knownThreat = null;
        _requireHidden = false;
        if (started)
        {
            LogEvent($"no cover (not managed): flight (fight chance {fightChance:0.00}: {why}), fleeing {fleeDistance:0} m");
            return true;
        }
        _forcedRetreat = false;
        LogEvent("no cover (not managed): nowhere to flee to, fights");
        return false;
    }

    /// <summary>Positive = fight, negative = flee. Soft: the result feeds a logistic chance, not a threshold.</summary>
    private float NoCoverFightScore(float distance, float ratio, out string why)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float score = fork.ShotNoCoverBias; // the roll already said "not fight": flight is the default
        string parts = $"base {score:0.0#}";
        if (IsCloseRange(distance))
        {
            score += fork.ShotNoCoverCloseBonus;
            parts += $", close +{fork.ShotNoCoverCloseBonus:0.0#}";
        }
        if (ratio <= 0.5f)
        {
            score += fork.ShotNoCoverNearBonus;
            parts += $", inside half reach +{fork.ShotNoCoverNearBonus:0.0#}";
        }
        else if (ratio <= 1f)
        {
            score += fork.ShotNoCoverReachBonus;
            parts += $", inside reach +{fork.ShotNoCoverReachBonus:0.0#}";
        }
        else if (ratio <= FIGHT_REACH_LIMIT)
        {
            score -= fork.ShotNoCoverJustOutPenalty;
            parts += $", just out of reach -{fork.ShotNoCoverJustOutPenalty:0.0#}";
        }
        else
        {
            score -= fork.ShotNoCoverFarPenalty;
            parts += $", far out of reach -{fork.ShotNoCoverFarPenalty:0.0#}";
        }
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                score += fork.ShotNoCoverPersonality;
                parts += $", aggressive +{fork.ShotNoCoverPersonality:0.0#}";
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                score -= fork.ShotNoCoverPersonality;
                parts += $", timid -{fork.ShotNoCoverPersonality:0.0#}";
                break;
        }
        ETagStatus health = Bot.Memory.Health.HealthStatus;
        if (health == ETagStatus.BadlyInjured || health == ETagStatus.Dying)
        {
            score -= fork.ShotNoCoverHurtPenalty;
            parts += $", hurt -{fork.ShotNoCoverHurtPenalty:0.0#}";
        }
        // (No "cannot sprint" bonus: MovementContext.CanSprint is also false for tired, healing or sprint-blocked bots, not just crippled legs.
        // A proper leg-damage test belongs to the crippling-damage work.)
        why = parts;
        return score;
    }

    private float _stallRefTime = -1f;
    private Vector3 _stallRefPos;

    /// <summary>
    /// True when the retreating bot has stayed within Min Progress of a reference point for Stall Time seconds (the reference moves with it whenever it makes progress).
    /// The note says how far it moved, how far the target still is and what the bot sees, for the one log line.
    /// </summary>
    private bool IsRetreatStalled(Enemy enemy, float now, out string note)
    {
        note = null;
        ForkSettings fork = GlobalSettings.Fork;
        if (fork.ShotRetreatStallTime <= 0f)
        {
            return false;
        }
        Vector3 position = Bot.Position;
        float minProgress = fork.ShotRetreatStallMinProgress;
        if (_stallRefTime < 0f || (position - _stallRefPos).sqrMagnitude >= minProgress * minProgress)
        {
            _stallRefTime = now;
            _stallRefPos = position;
            return false;
        }
        if (now - _stallRefTime < fork.ShotRetreatStallTime)
        {
            return false;
        }
        note = $"stalled (moved {Vector3.Distance(position, _stallRefPos):0.0} m in {now - _stallRefTime:0} s, {Vector3.Distance(position, RelocatePoint):0} m from the target, "
            + $"visible {enemy.IsVisible}, can shoot {enemy.CanShoot}, player at {enemy.RealDistance:0} m)";
        return true;
    }

    private bool ContinueRelocating(Enemy enemy)
    {
        float now = Time.time;
        bool arrived = (Bot.Position - RelocatePoint).sqrMagnitude < RELOCATE_ARRIVED_DISTANCE * RELOCATE_ARRIVED_DISTANCE;
        // A duel retreat goes on although the bot sees the player (that is the whole point of it).
        bool sees = !_forcedRetreat && SeesShooter(enemy) && enemy.CanShoot;
        // Fork: a retreat that makes no progress (stuck on a path, blocked) ends instead of running to the time limit with the bot standing still.
        string stallNote = null;
        bool stalled = !arrived && !sees && IsRetreatStalled(enemy, now, out stallNote);
        if (now >= _relocateEnd || arrived || sees || stalled || !Enabled)
        {
            string why = now >= _relocateEnd ? "time limit" : arrived ? "arrived" : !Enabled ? "switched off" : stalled ? stallNote : "the bot sees the shooter";
            StopRelocating(now, why);
            if (Enabled && (arrived || now >= _relocateEnd || stalled) && !sees)
            {
                StartPostRetreat(enemy, now);
            }
            return false;
        }
        return true;
    }

    private void StopRelocating(float now, string reason)
    {
        _forcedRetreat = false;
        LogEvent($"relocation over: {reason}");
        StartTrace(now);
        Bot.Cover.ClearShotState();
        Relocating = false;
        // Done: the shooter is forgotten (unless new information arrives) and the bot resumes its task from here.
    }

    /// <summary>
    /// The bot only fires suppressing bursts if it rolled Suppress (or the reaction is over).
    /// </summary>
    public bool AllowsSuppress(Enemy enemy)
    {
        if (!IsManaging(enemy))
        {
            return true;
        }
        // A Cover bot normally holds fire, but one that is being shot at from close range fires back from its cover.
        return Outcome != EShotReaction.Cover || IsCloseRange(_lastShotDistance);
    }

    /// <summary>
    /// With "Log Rolls" on: says why a shot or hit did not start a reaction (at most one line per bot every 2 seconds).
    /// </summary>
    private void LogSkipped(Enemy enemy, bool hit)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.ShotReactionLog || Time.time < _nextSkipLogTime)
        {
            return;
        }
        _nextSkipLogTime = Time.time + 2f;

        string reason;
        if (!fork.ShotReactionEnabled)
        {
            reason = "Shot Reaction is off";
        }
        else if (!Bot.Info.Profile.ShotReactionEnabled)
        {
            reason = $"brain filter is off for this bot type ({Bot.Info.Profile.ForkGroup}, {Bot.Info.Profile.WildSpawnType})";
        }
        else if (fork.ShotReactionPlayerOnly && enemy?.EnemyPlayer?.IsYourPlayer != true)
        {
            reason = "shooter is not you (Player Shooters Only is on)";
        }
        else if (SeesShooter(enemy))
        {
            reason = $"the bot has seen the shooter for {enemy.VisibleDuration:0.0} s";
        }
        else
        {
            reason = "no enemy";
        }
        Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Tag}: {(hit ? "hit" : "bullet")} ignored: {reason}");
    }

    /// <summary>Log label: the bot's name, its brain (spawn type) and, for a PMC, its nickname.</summary>
    public string Tag
    {
        get
        {
            var profile = Bot.Info.Profile;
            string nickname = Bot.BotOwner?.Profile?.Nickname;
            return profile.IsPMC && !string.IsNullOrEmpty(nickname) ? $"{Bot.name} [{profile.WildSpawnType} {nickname}]" : $"{Bot.name} [{profile.WildSpawnType}]";
        }
    }

    private void LogEvent(string message)
    {
        if (GlobalSettings.Fork.ShotReactionLog)
        {
            Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Tag}: {message}");
        }
    }

    /// <summary>Diagnostics: after a reaction ends, logs every 2 s for 60 s what drives the bot (active layer, node, reason) and how far it is from the shot area.</summary>
    private void StartTrace(float now)
    {
        if (!GlobalSettings.Fork.ShotReactionLog)
        {
            return;
        }
        _traceUntil = now + 60f;
        _nextTrace = now;
    }

    private readonly System.Collections.Generic.Queue<float> _hitTimes = new System.Collections.Generic.Queue<float>();
    private float _nextProximityCheck;
    private float _nextOtherEnemyCheck;
    private float _closeDiagUntil;
    private float _nextCloseDiag;

    /// <summary>After a close interrupt, logs once a second for 8 s whether the bot sees and can shoot the shooter and what it decided (diagnoses spotting delays).</summary>
    private void LogCloseDiagnostics(float now)
    {
        if (now >= _closeDiagUntil || now < _nextCloseDiag || _enemy == null)
        {
            return;
        }
        _nextCloseDiag = now + 1f;
        LogEvent(
            $"close fight: {(_enemy.EnemyPlayer != null ? (_enemy.EnemyPlayer.Position - Bot.Position).magnitude : -1f):0.0} m, visible {_enemy.IsVisible} ({_enemy.VisibleDuration:0.0} s), can shoot {_enemy.CanShoot}, "
                + $"goal enemy {(Bot.GoalEnemy == _enemy)}, decision {Bot.Decision.CurrentCombatDecision}"
        );
    }

    /// <summary>A state a close shooter can interrupt: hiding, holding cover firing, investigating or retreating. A fight is never interrupted.</summary>
    private bool Interruptible
    {
        get { return Relocating || Outcome == EShotReaction.Cover || Outcome == EShotReaction.FightOn || Outcome == EShotReaction.Hunt; }
    }

    private void InterruptToFight(Enemy enemy, float now, string why)
    {
        string state = Relocating ? "Retreat" : Outcome.ToString();
        LogEvent($"{why} while {state} is in effect: interrupted, fight");
        if (Relocating)
        {
            StopRelocating(now, "interrupted by a close shooter");
        }
        Bot.Cover.StopAdvance();
        Bot.Cover.ClearShotState();
        _hitTimes.Clear();
        Apply(enemy, EShotReaction.Fight, 0f, now, GlobalSettings.Fork);
        _closeDiagUntil = now + 8f;
        _nextCloseDiag = now;
    }

    /// <summary>
    /// Shot interrupts: a hit or bullet from the close interrupt distance, or enough hits within a short window while hiding or running, make the bot fight.
    /// </summary>
    private bool TryShotInterrupt(Enemy enemy, float distance, bool hit, float now)
    {
        if (!Interruptible)
        {
            return false;
        }
        ForkSettings fork = GlobalSettings.Fork;
        if (fork.ShotCloseInterruptDistance > 0f && distance <= fork.ShotCloseInterruptDistance)
        {
            InterruptToFight(enemy, now, $"{(hit ? "hit" : "bullet")} at {distance:0} m (close interrupt)");
            return true;
        }
        while (_hitTimes.Count > 0 && now - _hitTimes.Peek() > fork.ShotCorneredWindow)
        {
            _hitTimes.Dequeue();
        }
        if (fork.ShotCorneredHits > 0f && hit && distance <= fork.ShotCorneredDistance && _hitTimes.Count >= Mathf.RoundToInt(fork.ShotCorneredHits))
        {
            InterruptToFight(enemy, now, $"cornered: {_hitTimes.Count} hits in {fork.ShotCorneredWindow:0} s, last at {distance:0} m");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Another enemy (not the shooter) is visible close to the bot while a reaction is in effect: the reaction ends and normal combat handles it.
    /// </summary>
    private void TryOtherEnemyInterrupt(float now)
    {
        float limit = GlobalSettings.Fork.ShotOtherEnemyInterruptDistance;
        if (limit <= 0f || now < _nextOtherEnemyCheck || !Interruptible)
        {
            return;
        }
        _nextOtherEnemyCheck = now + 0.25f;
        var visible = Bot.EnemyController?.VisibleEnemies;
        if (visible == null)
        {
            return;
        }
        for (int i = 0; i < visible.Count; i++)
        {
            Enemy other = visible[i];
            if (other == null || other == _enemy || !Enemy.IsEnemyActive(other) || !other.IsVisible || other.RealDistance > limit)
            {
                continue;
            }
            InterruptForOtherEnemy(other, now, $"{other.EnemyName} is visible at {other.RealDistance:0} m");
            return;
        }
    }

    /// <summary>An enemy other than the shooter hit the bot while a reaction is in effect.</summary>
    public void OnOtherEnemyHit(Enemy enemy)
    {
        if (enemy == null || enemy == _enemy || GlobalSettings.Fork.ShotOtherEnemyInterruptDistance <= 0f || !Interruptible)
        {
            return;
        }
        InterruptForOtherEnemy(enemy, Time.time, $"{enemy.EnemyName} hit the bot");
    }

    private void InterruptForOtherEnemy(Enemy other, float now, string why)
    {
        string state = Relocating ? "Retreat" : Outcome.ToString();
        LogEvent($"{why} while {state} is in effect: reaction ended, normal combat");
        if (Relocating)
        {
            StopRelocating(now, "another enemy is close");
        }
        Bot.Cover.StopAdvance();
        Bot.Cover.ClearShotState();
        _hitTimes.Clear();
        if (Outcome != EShotReaction.None)
        {
            End("another enemy is close");
        }
    }

    /// <summary>The shooter is within the close interrupt distance without shooting (it walked up to the bot): the bot fights. Checked a few times a second.</summary>
    private void TryProximityInterrupt(float now)
    {
        float limit = GlobalSettings.Fork.ShotCloseInterruptDistance;
        if (limit <= 0f || now < _nextProximityCheck || !Interruptible || _enemy == null || !Enemy.IsEnemyActive(_enemy))
        {
            return;
        }
        _nextProximityCheck = now + 0.2f;
        var player = _enemy.EnemyPlayer;
        if (player == null)
        {
            return;
        }
        float distance = (player.Position - Bot.Position).magnitude;
        if (distance <= limit)
        {
            InterruptToFight(_enemy, now, $"the shooter is {distance:0.0} m away");
        }
    }

    // ---- Duel retreat: a long fight the bot cannot win ----

    private readonly Queue<float> _duelHits = new();
    private float _localPressureTime = -999f;
    private float _pressure;
    private float _pressureTarget = 75f;
    private float _pressureLastCheck;
    private float _nextPressureCheck;
    private float _pressureCooldownUntil;
    private bool _retreatDown;
    private bool _requireLower;
    private float _duelSeen;
    private float _duelTarget = 30f;
    private float _duelLastSeen;
    private string _duelSource = "firing";
    private float _duelLastCheck;
    private float _nextDuelCheck;
    private float _duelCooldownUntil;
    private float _duelLastHitPlayer = -999f;
    private bool _forcedRetreat;
    private int _duelRepeats;
    private float _lastDuelRetreat = -999f;
    private bool _longRetreat;
    private float _duelBase = 30f;
    private float _duelLastTrigger = -999f;
    private readonly List<float> _duelBursts = [];
    private bool _duelForceLeave;
    private string _duelForceReason = "";
    private float _nextHurtRoll;
    private bool _hurtLeave;
    private bool _hurtPending;
    private float _hurtPendingSince;
    private float _hurtCooldownUntil;
    private Vector3 _avoidPos;
    private float _avoidDistance;
    private readonly NavMeshPath _avoidPath = new();

    /// <summary>True when the walking path from a to b is not complete or passes within radius meters (horizontally) of point: a way out that leads through the player is no way out.</summary>
    private bool PathPassesNear(Vector3 a, Vector3 b, Vector3 point, float radius)
    {
        if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, _avoidPath) || _avoidPath.status != NavMeshPathStatus.PathComplete)
        {
            return true;
        }
        Vector3[] corners = _avoidPath.corners;
        Vector2 p = new(point.x, point.z);
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector2 s = new(corners[i].x, corners[i].z);
            Vector2 e = new(corners[i + 1].x, corners[i + 1].z);
            Vector2 d = e - s;
            float len = d.sqrMagnitude;
            float u = len > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - s, d) / len) : 0f;
            if ((p - (s + (d * u))).magnitude < radius)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A bot that is badly injured or dying, and sees the player (or is being hit), may break off to heal, at any range: it rolls every ShotHurtRollInterval seconds, weighted by how hurt
    /// it is, its resolve, whether it can heal and (at close range) ShotHurtCloseFactor. It goes to a place out of the player's sight, not far at close range (ShotHurtCloseDistanceMult), far at
    /// long range, and holds there at least ShotHurtMinHold seconds. A failed roll at range may become a push; otherwise it keeps fighting. It never starts mid-burst (it waits for a pause
    /// in its fire). A bot with no way out that does not pass within ShotHurtAvoidDistance of the player (trapped) stays and fights.
    /// </summary>
    private void TryHurtRetreat(Enemy enemy, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (fork.ShotHurtLeaveChance <= 0f || Relocating || now < _hurtCooldownUntil || OnStationary())
        {
            return;
        }
        ETagStatus health = Bot.Memory.Health.HealthStatus;
        if (health != ETagStatus.BadlyInjured && health != ETagStatus.Dying)
        {
            _hurtPending = false;
            return;
        }
        float distance = enemy.RealDistance;
        bool close = IsCloseRange(distance);
        if (!_hurtPending)
        {
            if (now < _nextHurtRoll || !IsEngaged(now))
            {
                return;
            }
            _nextHurtRoll = now + fork.ShotHurtRollInterval;
            float resolve = DuelResolve(enemy);
            bool meds = HasMeds();
            float chance = fork.ShotHurtLeaveChance * (health == ETagStatus.Dying ? 1f : 0.7f) * Mathf.Lerp(1.3f, 0.7f, resolve) * (meds ? 1f : fork.ShotHurtNoMedsFactor) * (close ? fork.ShotHurtCloseFactor : 1f);
            chance = Mathf.Clamp01(chance);
            if (Random.value < chance)
            {
                _hurtPending = true;
                _hurtPendingSince = now;
                LogEvent($"badly hurt ({health}), breaks off to heal (chance {chance:0.00}, can heal {meds}, resolve {resolve:0.00}, player at {distance:0} m{(close ? ", close range" : "")})");
            }
            else
            {
                float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
                float pull = DuelPull(now, distance);
                if (!close && distance <= reach && pull < 0.5f && Random.value < fork.ShotHurtPushChance)
                {
                    LogEvent($"badly hurt ({health}) but goes for the push anyway (leave chance was {chance:0.00})");
                    _duelSeen = 0f;
                    _duelBursts.Clear();
                    _duelCooldownUntil = now + 20f;
                    StartPush(enemy, now);
                }
                else
                {
                    LogEvent($"badly hurt ({health}) but keeps fighting (leave chance was {chance:0.00})");
                }
                return;
            }
        }
        // Never mid-burst: wait for a pause in its fire, at most ShotDuelWaitPause seconds.
        ShootData fireData = Bot.BotOwner.ShootData;
        if (fireData != null && now - fireData.LastTriggerPressd < fork.ShotDuelBurstGap && now - _hurtPendingSince < fork.ShotDuelWaitPause)
        {
            return;
        }
        _hurtPending = false;
        BreakOff(enemy, now, close, close ? fork.ShotHurtCloseDistanceMult : fork.ShotHurtDistanceMult, "to heal", true, WaitNote(now, _hurtPendingSince));
    }

    /// <summary>
    /// A break-off retreat out of the player's sight (hurt, or after a squadmate was killed). At close range it goes a short way and the path must not pass within ShotHurtAvoidDistance
    /// of the player; with no such way out (trapped) the bot stays and fights. Holds at least ShotHurtMinHold seconds when hurt, else the long retreat hold.
    /// </summary>
    private bool BreakOff(Enemy enemy, float now, bool close, float mult, string why, bool hurt, string waitNote, bool notifySquad = true)
    {
        ForkSettings fork = GlobalSettings.Fork;
        _enemy = enemy;
        _forcedRetreat = true;
        // At close range the way out must also end out of the player's sight (no running into the open); with none the bot stays and fights.
        // A bot that knows where the player is retreats out of his sight (always at close range, see ShotHurtCloseNeedsHidden; farther away ShotKnownRetreatNeedsHidden), against his real position.
        bool known = KnowsPosition(enemy, out Vector3 knownPosition);
        _avoidPos = knownPosition;
        _avoidDistance = close && known ? Mathf.Max(fork.ShotHurtAvoidDistance, 0f) : 0f;
        _requireHidden = known && (close ? fork.ShotHurtCloseNeedsHidden : fork.ShotKnownRetreatNeedsHidden);
        _knownThreat = known ? knownPosition : null;
        bool started = TryStartRelocating(ScaledRelocateDistance(mult));
        _knownThreat = null;
        _avoidDistance = 0f;
        _requireHidden = false;
        if (started)
        {
            _hurtCooldownUntil = now + fork.ShotDuelCooldown;
            _duelSeen = 0f;
            _duelBursts.Clear();
            _longRetreat = true;
            _hurtLeave = hurt;
            LogEvent($"breaks off {why}: retreats, distance x{mult:0.0}{(close ? " (close range, out of the player's sight, a way out that does not pass the player)" : "")}, {waitNote}");
            if (notifySquad)
            {
                OfferSquadFollow(now, ESquadFollow.Leave, hurt ? "hurt" : "squad loss", !hurt);
            }
            return true;
        }
        _forcedRetreat = false;
        _hurtCooldownUntil = now + 20f;
        LogEvent($"breaks off {why}: no way out{(close ? " that is out of the player's sight and does not pass the player" : "")}, stays and fights");
        return false;
    }

    /// <summary>For the log: how long the break-off waited for a pause in the fire, and whether it went anyway in the middle of a burst after the wait cap.</summary>
    private string WaitNote(float now, float since)
    {
        ShootData fireData = Bot.BotOwner.ShootData;
        bool firing = fireData != null && now - fireData.LastTriggerPressd < GlobalSettings.Fork.ShotDuelBurstGap;
        return $"waited {now - since:0.0} s for a pause{(firing ? ", went mid-burst after the wait cap" : "")}";
    }

    // ---- squad follow: one bot of a squad leaves or pushes, its mates in the same fight may roll to do the same

    private enum ESquadFollow
    {
        None,
        Leave,
        Push,
    }

    private ESquadFollow _followKind;
    private float _followDue;
    private string _followWhy = "";

    /// <summary>
    /// This bot of an ordinary squad or a Rogue squad starts a leave or a push: its squadmates in the same fight (within the squad role range, engaged) each roll to follow, after a short
    /// staggered delay. A leave pulls at most ShotSquadFollowMaxShare of the squad (at least one mate stays, unless the leave came from a squad death); a push is joined with a lower chance, and only
    /// by mates that could push themselves. A follower never pulls anyone else along (no chains).
    /// </summary>
    private void OfferSquadFollow(float now, ESquadFollow kind, string why, bool squadLoss)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float baseChance = kind == ESquadFollow.Leave ? fork.ShotSquadFollowLeaveChance : fork.ShotSquadFollowPushChance;
        if (baseChance <= 0f || Bot.Squad?.Members == null || (!IsOrdinarySquadBot(Bot.BotOwner) && !IsRogueOrGoon(Bot.BotOwner)))
        {
            return;
        }
        int size = 0;
        int leaving = 0;
        foreach (var member in Bot.Squad.Members.Values)
        {
            if (member == null || member.BotOwner == null || member.BotOwner.IsDead || member.ShotReaction == null)
            {
                continue;
            }
            size++;
            if (member == Bot || member.ShotReaction.Relocating || member.ShotReaction._followKind == ESquadFollow.Leave)
            {
                leaving++;
            }
        }
        if (size < 2)
        {
            return;
        }
        int maxLeavers = squadLoss ? size : Mathf.Clamp(Mathf.RoundToInt(size * fork.ShotSquadFollowMaxShare), 1, size - 1);
        float rangeSqr = fork.ShotSquadRoleRange * fork.ShotSquadRoleRange;
        foreach (var member in Bot.Squad.Members.Values)
        {
            if (member == null || member == Bot || member.BotOwner == null || member.BotOwner.IsDead || member.ShotReaction == null)
            {
                continue;
            }
            if (kind == ESquadFollow.Leave && leaving >= maxLeavers)
            {
                break;
            }
            if ((member.Position - Bot.Position).sqrMagnitude > rangeSqr)
            {
                continue;
            }
            if (member.ShotReaction.ReceiveFollow(now, kind, why, squadLoss) && kind == ESquadFollow.Leave)
            {
                leaving++;
            }
        }
    }

    private bool ReceiveFollow(float now, ESquadFollow kind, string why, bool squadLoss)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!Enabled || _followKind != ESquadFollow.None || Relocating || OnStationary() || now < _hurtCooldownUntil || _pushUntil > 0f)
        {
            return false;
        }
        Enemy enemy = Bot.GoalEnemy;
        if (enemy == null || enemy.IsAI || !Enemy.IsEnemyActive(enemy) || (fork.ShotReactionPlayerOnly && enemy.EnemyPlayer?.IsYourPlayer != true) || !IsEngaged(now))
        {
            return false;
        }
        float distance = enemy.RealDistance;
        float resolve = DuelResolve(enemy);
        float chance;
        if (kind == ESquadFollow.Leave)
        {
            // Low resolve follows more; a mate that merely got hurt is less of a reason, a squad death more of one.
            chance = fork.ShotSquadFollowLeaveChance * Mathf.Lerp(1.4f, 0.6f, resolve) * (why == "hurt" ? 0.5f : 1f) * (squadLoss ? 1.4f : 1f);
        }
        else
        {
            float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
            if (IsCloseRange(distance) || distance > reach || DuelPull(now, distance) >= 0.5f)
            {
                return false;
            }
            chance = fork.ShotSquadFollowPushChance * Mathf.Lerp(0.6f, 1.4f, resolve);
        }
        if (Random.value >= Mathf.Clamp01(chance))
        {
            return false;
        }
        _followKind = kind;
        _followWhy = why;
        _followDue = now + Random.Range(fork.ShotSquadFollowDelayMin, Mathf.Max(fork.ShotSquadFollowDelayMin, fork.ShotSquadFollowDelayMax));
        LogEvent($"squadmate {(kind == ESquadFollow.Leave ? "leaves" : "pushes")} ({why}): follows in {_followDue - now:0.0} s (chance {chance:0.00}, resolve {resolve:0.00})");
        return true;
    }

    private void TickSquadFollow(float now)
    {
        if (_followKind == ESquadFollow.None || now < _followDue)
        {
            return;
        }
        ForkSettings fork = GlobalSettings.Fork;
        Enemy enemy = Bot.GoalEnemy;
        if (enemy == null || enemy.IsAI || !Enemy.IsEnemyActive(enemy) || Relocating || OnStationary() || !IsEngaged(now))
        {
            _followKind = ESquadFollow.None;
            return;
        }
        // Never mid-burst: wait for a pause in its fire, at most ShotDuelWaitPause seconds.
        ShootData fireData = Bot.BotOwner.ShootData;
        if (fireData != null && now - fireData.LastTriggerPressd < fork.ShotDuelBurstGap && now - _followDue < fork.ShotDuelWaitPause)
        {
            return;
        }
        ESquadFollow kind = _followKind;
        _followKind = ESquadFollow.None;
        float distance = enemy.RealDistance;
        bool close = IsCloseRange(distance);
        if (kind == ESquadFollow.Leave)
        {
            BreakOff(enemy, now, close, close ? fork.ShotHurtCloseDistanceMult : fork.ShotDuelDistanceMult, $"to follow a squadmate ({_followWhy})", false, WaitNote(now, _followDue), false);
            return;
        }
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        if (close || distance > reach)
        {
            return;
        }
        LogEvent($"squadmate pushed: joins the push, {distance:0} m from the player");
        _duelSeen = 0f;
        _duelBursts.Clear();
        _duelCooldownUntil = now + 20f;
        StartPush(enemy, now, true);
    }

    private bool _squadClosePending;
    private float _squadClosePendingSince;
    private bool _squadLossCloseHandled;

    /// <summary>
    /// A squadmate was killed by the player and the fight is at close range (where the duel does not run): the survivor may break off, with a lower chance (ShotSquadLossCloseFactor), only in a
    /// pause in its fire, only with a way out that does not pass the player. Not for a bot without squadmates (nobody was lost). Farther away the duel handles squad losses.
    /// </summary>
    private void TrySquadLossCloseRetreat(Enemy enemy, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (fork.ShotSquadLossCloseFactor <= 0f || fork.ShotDuelSquadLossPenalty <= 0f || Relocating || OnStationary() || now < _hurtCooldownUntil)
        {
            return;
        }
        if (now - _squadLossTime >= fork.ShotDuelSquadLossTime)
        {
            _squadClosePending = false;
            return;
        }
        float distance = enemy.RealDistance;
        if (!IsCloseRange(distance))
        {
            _squadClosePending = false;
            return;
        }
        if (!_squadClosePending)
        {
            if (_squadLossCloseHandled || !IsEngaged(now))
            {
                return;
            }
            _squadLossCloseHandled = true;
            float resolve = DuelResolve(enemy);
            float chance = Mathf.Clamp01(Mathf.Lerp(0.85f, 0.4f, resolve) * fork.ShotSquadLossCloseFactor);
            if (Random.value >= chance)
            {
                LogEvent($"a squadmate was killed, close range ({distance:0} m): keeps fighting (chance {chance:0.00}, resolve {resolve:0.00})");
                return;
            }
            _squadClosePending = true;
            _squadClosePendingSince = now;
            LogEvent($"a squadmate was killed, close range ({distance:0} m): breaks off (chance {chance:0.00}, resolve {resolve:0.00})");
        }
        ShootData fireData = Bot.BotOwner.ShootData;
        if (fireData != null && now - fireData.LastTriggerPressd < fork.ShotDuelBurstGap && now - _squadClosePendingSince < fork.ShotDuelWaitPause)
        {
            return;
        }
        _squadClosePending = false;
        BreakOff(enemy, now, true, fork.ShotHurtCloseDistanceMult, "after a squadmate was killed", false, WaitNote(now, _squadClosePendingSince));
    }
    private float _duelDueSince = -1f;

    /// <summary>The bot has something to heal with (first aid or a surgical kit it would use now): SAIN's own healing can run in cover after a retreat.</summary>
    private bool HasMeds()
    {
        var medicine = Bot.BotOwner?.Medecine;
        return medicine != null && (medicine.FirstAid?.ShallStartUse() == true || medicine.SurgicalKit?.ShallStartUse() == true);
    }
    private float _duelExposure;
    private float _duelFlavor;
    private float _squadLossTime = -999f;
    private bool _squadLossHandled;
    private float _nextDuelLog;

    /// <summary>Seconds without seeing the player and firing after which the duel is forgotten (the clock resets): longer for marksman and sniper rifles, which fire slowly.</summary>
    private float DuelResetTime()
    {
        ForkSettings fork = GlobalSettings.Fork;
        return IsLongRifle() ? Mathf.Max(fork.ShotDuelResetTimeLongRifle, fork.ShotDuelResetTime) : fork.ShotDuelResetTime;
    }

    private bool IsLongRifle()
    {
        EWeaponClass weaponClass = Bot.Info.WeaponInfo.EWeaponClass;
        return weaponClass == EWeaponClass.marksmanRifle || weaponClass == EWeaponClass.sniperRifle;
    }

    /// <summary>How much a missed burst counts for this bot's weapon class: a sniper rifle or marksman rifle (SVD, DMR) shoots deliberate shots, a miss says more. Other classes count 1.</summary>
    private float DuelWeaponWeight()
    {
        ForkSettings fork = GlobalSettings.Fork;
        switch (Bot.Info.WeaponInfo.EWeaponClass)
        {
            case EWeaponClass.sniperRifle:
                return Mathf.Max(fork.ShotDuelWeightSniper, 1f);
            case EWeaponClass.marksmanRifle:
                return Mathf.Max(fork.ShotDuelWeightMarksman, 1f);
            default:
                return 1f;
        }
    }

    /// <summary>0 at ShotDuelLongStart (share of the weapon's reach) up to 1 at ShotDuelLongFull: how much the long range rules (weapon weights, sniper exposure) count. Near zero up close: short fights stay as they were.</summary>
    private float LongFade(float distance)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        return Mathf.Clamp01(Mathf.InverseLerp(fork.ShotDuelLongStart, Mathf.Max(fork.ShotDuelLongFull, fork.ShotDuelLongStart + 0.05f), distance / reach));
    }

    /// <summary>
    /// Scale of the duel time by distance (share of the weapon's reach): up to ShotDuelLongStart it stretches from ShotDuelShortTimeFactor (point blank) down to 1, so short and mid fights
    /// almost never end in a leave; beyond it shrinks to ShotDuelLongMinFactor at ShotDuelLongEnd.
    /// </summary>
    private float LongTimeFactor(float distance)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        float ratio = distance / reach;
        float start = Mathf.Max(fork.ShotDuelLongStart, 0.05f);
        if (ratio < start)
        {
            return Mathf.Lerp(Mathf.Max(fork.ShotDuelShortTimeFactor, 1f), 1f, ratio / start);
        }
        float t = Mathf.Clamp01(Mathf.InverseLerp(start, Mathf.Max(fork.ShotDuelLongEnd, start + 0.05f), ratio));
        return Mathf.Lerp(1f, Mathf.Clamp(fork.ShotDuelLongMinFactor, 0.1f, 1f), t);
    }

    /// <summary>A squadmate of this bot was killed (by the player): its resolve drops for a while (see DuelResolve) and it may decide at once (see TryDuelRetreat).</summary>
    public void NoteSquadLoss(float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!Enabled || !fork.ShotDuelRetreatEnabled || fork.ShotDuelSquadLossPenalty <= 0f)
        {
            return;
        }
        _squadLossTime = now;
        _squadLossHandled = false;
        _squadLossCloseHandled = false;
        _squadClosePending = false;
    }

    private string DuelWhat()
    {
        return _duelSource == "firing" ? "seen firing" : $"in combat ({_duelSource})";
    }

    private string DuelHas()
    {
        return _duelSource == "firing" ? "has been firing at the player" : $"has been in combat with the player ({_duelSource})";
    }

    private void DuelLog(float now, string note, Enemy enemy, float resolve, float pull)
    {
        if (!GlobalSettings.Fork.ShotDuelLog || now < _nextDuelLog)
        {
            return;
        }
        _nextDuelLog = now + 15f;
        float distance = enemy != null ? enemy.RealDistance : 0f;
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        LogEvent($"duel clock: {DuelWhat()} {_duelSeen:0} s of {_duelTarget:0} s, {note}, resolve {resolve:0.00}, pull {pull:0.00} ({_duelBursts.Count} missed bursts, {_duelHits.Count} hits or close bullets), weapon {Bot.Info.WeaponInfo.EWeaponClass} x{DuelWeaponWeight():0.#}, player at {distance:0} m ({distance / reach * 100f:0}% of reach)");
    }

    /// <summary>
    /// Counts the bot's bursts at the player while a duel clock runs: a burst is a run of trigger pulls with gaps under ShotDuelBurstGap, however long it is, and counts once.
    /// Checked every frame (a float compare), because the 1 s duel tick would miss bursts. A hit by the bot on the player clears them (see OnBotHitEnemy).
    /// </summary>
    private void TrackDuelBursts(float now)
    {
        if (_duelSeen <= 0f)
        {
            return;
        }
        ShootData shootData = Bot.BotOwner?.ShootData;
        if (shootData == null)
        {
            return;
        }
        float trigger = shootData.LastTriggerPressd;
        if (trigger <= _duelLastTrigger + 0.001f)
        {
            return;
        }
        ForkSettings fork = GlobalSettings.Fork;
        if (trigger - _duelLastTrigger > fork.ShotDuelBurstGap)
        {
            _duelBursts.Add(now);
        }
        _duelLastTrigger = trigger;
    }

    private float _pushUntil;
    private Enemy _pushEnemy;
    private float _pushRunUntil;
    private float _pushStart;
    private bool _pushSquadLossChecked;

    private void StartPush(Enemy enemy, float now, bool follow = false)
    {
        long started = ShotPerf.Start();
        StartPushCore(enemy, now, follow);
        ShotPerf.Stop(ShotPerf.Push, started);
    }

    private void StartPushCore(Enemy enemy, float now, bool follow)
    {
        _pushStart = now;
        _pushSquadLossChecked = false;
        _pushEnemy = enemy;
        _pushUntil = now + GlobalSettings.Fork.ShotDuelPushTime;
        _pushRunUntil = 0f;
        _noHopUntil = 0f;
        _hopBestDistance = float.MaxValue;
        _hopProgressTime = now;
        _hopHold = HopHoldTime();
        if (!follow)
        {
            OfferSquadFollow(now, ESquadFollow.Push, "duel push", false);
        }
    }

    private void EndPush(string why)
    {
        if (_pushUntil <= 0f)
        {
            return;
        }
        _pushUntil = 0f;
        _pushEnemy = null;
        _pushRunUntil = 0f;
        Bot.Cover.StopAdvance();
        SprintWhileInvestigating = false;
        LogEvent($"duel push over: {why}");
    }

    /// <summary>
    /// The duel objective is to push: cover to cover toward the player with the investigate rules (hold each cover a few seconds, sprint to the next one that is closer),
    /// until it is inside part of its weapon's reach, the time runs out, there is no closer cover, or the pull to leave grows (it is losing: then it retreats after all).
    /// False = the normal SAIN decisions run (the bot fights from where it is).
    /// </summary>
    private bool TryPushDecision(Enemy enemy, float now, out ECombatDecision decision)
    {
        decision = ECombatDecision.None;
        ForkSettings fork = GlobalSettings.Fork;
        if (enemy != _pushEnemy)
        {
            return false;
        }
        if (!Enemy.IsEnemyActive(_pushEnemy) || Relocating || Outcome != EShotReaction.None || OnStationary())
        {
            EndPush(Relocating ? "another relocation" : "enemy gone or another reaction");
            return false;
        }
        if (now >= _pushUntil)
        {
            EndPush("time is up");
            return false;
        }
        float distance = enemy.RealDistance;
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        if (IsCloseRange(distance) || distance <= reach * fork.ShotDuelPushStopReach)
        {
            EndPush($"close enough ({distance:0} m, reach {reach:0} m)");
            return false;
        }
        float pull = DuelPull(now, distance);
        if (pull >= fork.ShotDuelPushAbortPull)
        {
            EndPush($"it is losing (pull {pull:0.00}), retreats instead");
            // The duel clock is over at once: the next duel tick turns it into a retreat.
            _duelSeen = Mathf.Max(_duelTarget, 1f);
            _duelCooldownUntil = 0f;
            return false;
        }
        // A squadmate killed during the push (at any range outside close range) may break it: usually, not always, more for a bot with low resolve.
        if (!_pushSquadLossChecked && _squadLossTime > _pushStart && now - _squadLossTime < fork.ShotDuelSquadLossTime)
        {
            _pushSquadLossChecked = true;
            float pushResolve = DuelResolve(enemy);
            float breakChance = Mathf.Lerp(0.8f, 0.3f, pushResolve);
            if (Random.value < breakChance)
            {
                EndPush($"a squadmate was killed (chance {breakChance:0.00}, resolve {pushResolve:0.00}), it leaves instead");
                _duelSeen = Mathf.Max(_duelTarget, 1f);
                _duelForceLeave = true;
                _duelForceReason = "squad loss";
                _duelCooldownUntil = 0f;
                return false;
            }
        }
        SprintWhileInvestigating = false;
        Vector3 area = enemy.EnemyPosition;
        if (Bot.Cover.AdvanceActive)
        {
            float hopAge = now - Bot.Cover.AdvanceStartTime;
            bool stalled = hopAge > HOP_STALL_TIME && !Bot.Mover.Moving && Bot.Cover.CoverPoint_MovingTo == null;
            var hopTarget = Bot.Cover.CoverPoint_MovingTo;
            if (hopTarget != null)
            {
                float hopDistance = Vector3.Distance(Bot.Position, hopTarget.Position);
                if (hopDistance < _hopBestDistance - 1f)
                {
                    _hopBestDistance = hopDistance;
                    _hopProgressTime = now;
                }
            }
            bool noProgress = now - _hopProgressTime > fork.ShotHopNoProgressTime;
            if (hopAge <= fork.ShotHopMaxTime && !stalled && !noProgress)
            {
                decision = ECombatDecision.SeekCover;
                return true;
            }
            Bot.Cover.StopAdvance();
            _noHopUntil = now + HOP_RETRY_DELAY;
        }
        bool inCover = Bot.Cover.CoverInUse != null;
        if (inCover && Bot.Cover.TimeHeldInCover < _hopHold)
        {
            decision = ECombatDecision.SeekCover;
            return true;
        }
        if (now >= _noHopUntil && Bot.Cover.StartAdvanceHop(area))
        {
            _hopBestDistance = float.MaxValue;
            _hopProgressTime = now;
            _hopHold = HopHoldTime();
            _pushRunUntil = 0f;
            LogEvent($"duel push: next cover closer to the player ({distance:0} m away)");
            decision = ECombatDecision.SeekCover;
            return true;
        }
        if (_pushRunUntil > 0f)
        {
            if (now < _pushRunUntil)
            {
                SprintWhileInvestigating = true;
                decision = ECombatDecision.Search;
                return true;
            }
            EndPush("the run is over");
            return false;
        }
        if (!inCover)
        {
            // No closer cover and in the open: a short sprint toward the player.
            _pushRunUntil = now + fork.ShotAdvanceNoCoverTime;
            LogEvent($"duel push: no closer cover, sprinting toward the player for up to {fork.ShotAdvanceNoCoverTime:0} s");
            SprintWhileInvestigating = true;
            decision = ECombatDecision.Search;
            return true;
        }
        EndPush("no closer cover");
        return false;
    }

    /// <summary>The pull to leave (0..0.8): bursts that missed in the miss window and hits or close bullets of the player on this bot.</summary>
    private float DuelPull(float now, float distance)
    {
        ForkSettings fork = GlobalSettings.Fork;
        _duelBursts.RemoveAll(time => now - time > fork.ShotDuelMissWindow);
        // A sniper or marksman rifle counts more per miss, fading in with the distance as a share of its reach (1x up close).
        float weight = Mathf.Lerp(1f, DuelWeaponWeight(), LongFade(distance));
        return Mathf.Clamp(((_duelBursts.Count * fork.ShotDuelMissWeight) + (_duelHits.Count * fork.ShotDuelHitWeight)) * weight, 0f, 0.8f);
    }

    /// <summary>
    /// How stubborn this bot is in a duel (0..1): personality, the weapon's reach against the distance, a squad nearby and its health.
    /// High = keeps fighting (and may push), low = gives the position up sooner and runs further.
    /// </summary>
    private float DuelResolve(Enemy enemy)
    {
        float resolve = 0.5f;
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                resolve += 0.2f;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                resolve -= 0.2f;
                break;
        }
        float reach = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        float ratio = enemy.RealDistance / reach;
        // Outranged: up to -0.3 at twice its reach or more; well inside its reach: +0.1.
        resolve += ratio > 1f ? -0.3f * Mathf.Clamp01(ratio - 1f) : (ratio < 0.7f ? 0.1f : 0f);
        ETagStatus health = Bot.Memory.Health.HealthStatus;
        if (health == ETagStatus.BadlyInjured || health == ETagStatus.Dying)
        {
            resolve -= 0.3f;
        }
        int mates = 0;
        if (Bot.Squad?.Members != null)
        {
            foreach (var member in Bot.Squad.Members.Values)
            {
                if (member != null && member != Bot && member.BotOwner != null && !member.BotOwner.IsDead && (member.Position - Bot.Position).sqrMagnitude <= 2500f)
                {
                    mates++;
                }
            }
        }
        resolve += mates > 0 ? 0.1f : -0.1f;
        // The shooter's equipment: a suppressed or prone player is harder to place, so the bot is less sure it can win.
        if (enemy.EnemyPlayerComponent?.Equipment?.CurrentWeaponInfo?.HasSuppressor == true)
        {
            resolve -= 0.05f;
        }
        if (enemy.EnemyPlayer != null && enemy.EnemyPlayer.IsInPronePose)
        {
            resolve -= 0.05f;
        }
        // A kill in the squad shakes the survivors (not when the fight is close), timid ones more, aggressive ones less.
        ForkSettings fork = GlobalSettings.Fork;
        if (Time.time - _squadLossTime < fork.ShotDuelSquadLossTime && ratio >= fork.ShotDuelSquadLossMinShare)
        {
            float penalty = fork.ShotDuelSquadLossPenalty;
            switch (Bot.Info.Personality)
            {
                case EPersonality.Wreckless:
                case EPersonality.GigaChad:
                case EPersonality.Chad:
                    penalty *= 0.7f;
                    break;
                case EPersonality.Rat:
                case EPersonality.Timmy:
                case EPersonality.Coward:
                    penalty *= 1.3f;
                    break;
            }
            resolve -= penalty;
        }
        // A small per-engagement random offset: the same bot in the same spot does not always decide the same.
        resolve += _duelFlavor;
        return Mathf.Clamp01(resolve);
    }

    /// <summary>A distance for a retreat that is a multiple of the normal relocate distance (a random value between the relocate minimum and maximum).</summary>
    private static float ScaledRelocateDistance(float mult)
    {
        ForkSettings fork = GlobalSettings.Fork;
        float min = Mathf.Min(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
        float max = Mathf.Max(fork.ShotRelocateMinDistance, fork.ShotRelocateMaxDistance);
        return Random.Range(min, max) * Mathf.Max(mult, 0.1f);
    }

    /// <summary>
    /// Rogues and Goons: the player keeps shooting near them (hits and close bullets at them or at Rogues and Goons within the shared radius) for a random
    /// RoguePressureTimeMin to RoguePressureTimeMax seconds (a gap over RoguePressureGap resets the clock) without the bot seeing the player: it cannot pinpoint him and gives the position up, a full retreat down
    /// off the roof (RogueRetreatDown). A window for the sniper, then the Rogues are gone. A gunner leaves its gun. Once per RoguePressureCooldown.
    /// </summary>
    private void TryPressureRetreat(float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.RoguePressureRetreatEnabled || now < _nextPressureCheck || !Enabled || !IsRogueOrGoon(Bot.BotOwner))
        {
            return;
        }
        float dt = Mathf.Min(now - _pressureLastCheck, 2f);
        _pressureLastCheck = now;
        _nextPressureCheck = now + 1f;
        // Local pressure only: hits and close bullets (not shots it merely hears) at it or at Rogues and Goons within the shared radius. Global alerts and far reports do not count,
        // so the retreat reaches the Rogues near the shooting, not all of them.
        bool pressure = now - _localPressureTime <= fork.RoguePressureGap;
        Enemy goal = Bot.GoalEnemy;
        bool sees = goal != null && goal.IsVisible && goal.CanShoot;
        if (!pressure)
        {
            _pressure = 0f;
            return;
        }
        if (sees)
        {
            // A bot that really sees the player duels instead (see TryDuelRetreat).
            return;
        }
        if (_pressure <= 0f)
        {
            _pressureTarget = Random.Range(fork.RoguePressureTimeMin, Mathf.Max(fork.RoguePressureTimeMin, fork.RoguePressureTimeMax));
        }
        _pressure += dt;
        if (_pressure < _pressureTarget || now < _pressureCooldownUntil || Relocating || _vaguePositionTime <= 0f)
        {
            return;
        }
        Enemy enemy = _enemy ?? goal;
        if (enemy == null)
        {
            return;
        }
        float seconds = _pressure;
        _enemy = enemy;
        _forcedRetreat = true;
        _retreatDown = fork.RogueRetreatDown;
        BotStationaryWeaponData stationary = StationaryData;
        if (stationary != null && (stationary.Taken || stationary.WantToTake) && stationary.CurLink != null)
        {
            AbandonStationary(stationary, now, "pressure retreat");
        }
        bool started = TryStartRelocating(ScaledRelocateDistance(fork.RogueRetreatDistanceMult));
        _retreatDown = false;
        _pressure = 0f;
        if (started)
        {
            _pressureCooldownUntil = now + fork.RoguePressureCooldown;
            _longRetreat = true;
            LogEvent($"pressure retreat: {seconds:0} s of information about the player without seeing him (limit {_pressureTarget:0} s), going down: {fork.RogueRetreatDown}, retreat distance x{fork.RogueRetreatDistanceMult:0.0}");
            return;
        }
        _forcedRetreat = false;
        _pressureCooldownUntil = now + 30f;
    }

    /// <summary>The player hit this bot, or a bullet of the player passed close to it (seen or not): counts toward a duel retreat.</summary>
    private void NoteDuelHit(Enemy enemy, bool hit)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (enemy == null || !fork.ShotDuelRetreatEnabled || !Enabled || (fork.ShotReactionPlayerOnly && enemy.EnemyPlayer?.IsYourPlayer != true))
        {
            return;
        }
        _duelHits.Enqueue(Time.time);
        _lastDuelHitTime = Time.time;
        // A sniper or marksman that is spotted and shot at wants to leave (a sniper position is only worth something unseen), but not every time: each hit or close bullet
        // is a roll, weighted by the bot's resolve (personality, equipment, health, squad) and the distance, and earlier exposures add to the next roll.
        if (_duelSeen > 0f && !_duelForceLeave && IsLongRifle())
        {
            float distance = enemy.RealDistance;
            float fade = LongFade(distance);
            if (fade > 0f)
            {
                float resolve = DuelResolve(enemy);
                float chance = ((hit ? fork.ShotDuelSniperHitChance : fork.ShotDuelSniperCloseChance) * fade * Mathf.Lerp(1.6f, 0.6f, resolve)) + (_duelExposure * 0.08f);
                _duelExposure += hit ? 1f : 0.5f;
                if (Random.value < chance)
                {
                    _duelForceLeave = true;
                    _duelForceReason = "spotted, sniper rule";
                    LogEvent($"duel: spotted and {(hit ? "hit" : "shot at")} at {distance:0} m with a {Bot.Info.WeaponInfo.EWeaponClass}, it gives the position up (chance {chance:0.00}, resolve {resolve:0.00})");
                }
            }
        }
    }

    /// <summary>
    /// A bot that has been firing at the player for a random ShotDuelTimeMin to ShotDuelTimeMax seconds while seeing it (gaps under 15 s) without hitting it once
    /// (a hit resets the clock) gives the position up: a full retreat out of sight, then the normal hold and investigation. It may also need ShotDuelShots hits or close bullets
    /// of the player in the last ShotDuelWindow seconds (0 = the player does not have to shoot at it). Not at close range, once per ShotDuelCooldown.
    /// Gunners on a mounted gun are not affected (they leave it when it cannot answer).
    /// </summary>
    private void TryDuelRetreat(float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        if (!fork.ShotDuelRetreatEnabled || now < _nextDuelCheck || !Enabled)
        {
            return;
        }
        float dt = Mathf.Min(now - _duelLastCheck, 2f);
        _duelLastCheck = now;
        _nextDuelCheck = now + 1f;
        while (_duelHits.Count > 0 && now - _duelHits.Peek() > fork.ShotDuelWindow)
        {
            _duelHits.Dequeue();
        }
        Enemy enemy = Bot.GoalEnemy;
        if (enemy == null || enemy.IsAI || !Enemy.IsEnemyActive(enemy) || (fork.ShotReactionPlayerOnly && enemy.EnemyPlayer?.IsYourPlayer != true))
        {
            if (now - _duelLastSeen > DuelResetTime())
            {
                _duelSeen = 0f;
            }
            return;
        }
        long hurtStart = ShotPerf.Start();
        TryHurtRetreat(enemy, now);
        TrySquadLossCloseRetreat(enemy, now);
        ShotPerf.Stop(ShotPerf.HurtLoss, hurtStart);
        // At close range it is a fight, not a duel: the clock does not run (it would otherwise retreat the moment the range opens).
        if (IsCloseRange(enemy.RealDistance))
        {
            _duelSeen = 0f;
            _duelBursts.Clear();
            return;
        }
        // The duel clock runs while the bot is in combat with the player: it sees him and is firing (a trigger pull in the last few seconds); or, while the shot reaction is not
        // managing the bot (the two never overlap), he hit it or put a bullet close to it in the last 10 s and it has visuals on him (sees him or saw him in the last 8 s: a bot that
        // never saw him is the shot reaction's), or it sees him but cannot hit him (out of reach, not firing: faster). A hit by the bot on the player resets it.
        ShootData shootData = Bot.BotOwner.ShootData;
        bool firing = shootData != null && now - shootData.LastTriggerPressd < 8f;
        string source = null;
        float tickRate = 1f;
        if (enemy.IsVisible && firing)
        {
            source = "firing";
        }
        else if (fork.ShotDuelTicksInCombat && !InReaction)
        {
            if (enemy.IsVisible && enemy.RealDistance > Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f))
            {
                source = "cannot reach";
                tickRate = Mathf.Max(1f, fork.ShotDuelCantReachSpeed);
            }
            else if (now - _lastDuelHitTime < 10f && KnowsPosition(enemy, out _))
            {
                source = "engaged by shots";
            }
        }
        if (source != null)
        {
            if (_duelSeen <= 0f)
            {
                _duelBase = Random.Range(fork.ShotDuelTimeMin, Mathf.Max(fork.ShotDuelTimeMin, fork.ShotDuelTimeMax));
                _duelBursts.Clear();
                _duelLastTrigger = shootData != null ? shootData.LastTriggerPressd : -999f;
                _duelFlavor = Random.Range(-fork.ShotDuelFlavor, fork.ShotDuelFlavor);
                _duelExposure = 0f;
                _duelForceLeave = false;
                _duelForceReason = "";
                _duelDueSince = -1f;
                if (fork.ShotDuelLog)
                {
                    LogEvent($"duel clock starts: weapon {Bot.Info.WeaponInfo.EWeaponClass}, player at {enemy.RealDistance:0} m, base time {_duelBase:0} s");
                }
            }
            _duelSource = source;
            _duelSeen = Mathf.Min(_duelSeen + (dt * tickRate), 600f);
            _duelLastSeen = now;
        }
        else if (now - _duelLastSeen > DuelResetTime())
        {
            _duelSeen = 0f;
        }
        if (_duelSeen <= 0f)
        {
            return;
        }
        float distance = enemy.RealDistance;
        float reachNow = Mathf.Max(Bot.Info.WeaponInfo.EffectiveWeaponDistance, 1f);
        float resolve = DuelResolve(enemy);
        float pull = DuelPull(now, distance);
        if (Relocating || now < _duelCooldownUntil)
        {
            DuelLog(now, Relocating ? "blocked: already relocating" : $"blocked: cooldown, {_duelCooldownUntil - now:0} s left", enemy, resolve, pull);
            return;
        }
        // The limit: the random base time, longer for a stubborn bot, cut by the pull of missed bursts and hits on it and shorter at long range, never under the floor.
        float timeScale = LongTimeFactor(distance);
        _duelTarget = Mathf.Max(fork.ShotDuelTimeFloor * Mathf.Max(timeScale, 1f), _duelBase * Mathf.Lerp(0.6f, 1.4f, resolve) * (1f - pull) * timeScale);
        // A kill in the squad makes a survivor decide (once per loss, and not when the fight is close): usually, not always.
        if (!_squadLossHandled && now - _squadLossTime < fork.ShotDuelSquadLossTime && distance / reachNow >= fork.ShotDuelSquadLossMinShare)
        {
            _squadLossHandled = true;
            float decideChance = Mathf.Lerp(0.85f, 0.4f, resolve);
            if (Random.value < decideChance)
            {
                _duelSeen = Mathf.Max(_duelSeen, _duelTarget);
                LogEvent($"duel: a squadmate was killed, it decides now (chance {decideChance:0.00}, resolve {resolve:0.00})");
            }
        }
        bool forced = _duelForceLeave;
        if (!forced && (_duelSeen < _duelTarget || _duelHits.Count < Mathf.RoundToInt(fork.ShotDuelShots)))
        {
            DuelLog(now, "keeps fighting", enemy, resolve, pull);
            return;
        }
        if (IsCloseRange(distance) || OnStationary())
        {
            return;
        }
        // Never mid-burst: a leave or push only starts in a pause of its fire (a gap longer than the burst gap), waiting at most ShotDuelWaitPause seconds for one.
        if (_duelDueSince < 0f)
        {
            _duelDueSince = now;
        }
        ShootData fireData = Bot.BotOwner.ShootData;
        if (fireData != null && now - fireData.LastTriggerPressd < fork.ShotDuelBurstGap && now - _duelDueSince < fork.ShotDuelWaitPause)
        {
            DuelLog(now, "decided, waits for a pause in its fire", enemy, resolve, pull);
            return;
        }
        // Push or leave: a chance that grows with the resolve and falls with the pull, then a small flavor flip, so it is never fully predictable.
        float pushChance = Mathf.Clamp01(Mathf.InverseLerp(fork.ShotDuelPushResolve - 0.15f, fork.ShotDuelPushResolve + 0.15f, resolve)) * (1f - Mathf.Clamp01(pull / 0.3f));
        bool push = !forced && distance <= reachNow && KnowsPosition(enemy, out _) && Random.value < pushChance;
        if (!forced && Random.value < fork.ShotDuelFlavor * 0.6f)
        {
            push = !push && distance <= reachNow && KnowsPosition(enemy, out _);
        }
        if (push)
        {
            // Winning, or at least not losing: the objective is to push. It goes cover to cover toward the player (see TryPushDecision) instead of retreating.
            LogEvent($"duel push: resolve {resolve:0.00}, pull {pull:0.00}, {_duelBursts.Count} missed bursts, push chance {pushChance:0.00}, player at {distance:0} m, goes cover to cover toward it for up to {fork.ShotDuelPushTime:0} s");
            _duelSeen = 0f;
            _duelBursts.Clear();
            _duelCooldownUntil = now + 20f;
            StartPush(enemy, now);
            return;
        }
        float seen = _duelSeen;
        int hits = _duelHits.Count;
        _enemy = enemy;
        _forcedRetreat = true;
        // A bot that insists on shooting relocates further: the distance is a multiple of the normal one, and grows with every repeat inside the repeat window.
        if (now - _lastDuelRetreat > fork.ShotDuelRepeatWindow)
        {
            _duelRepeats = 0;
        }
        // Stubborn bots relocate shorter, timid ones and those taking a beating further.
        int missed = _duelBursts.Count;
        float mult = Mathf.Min((fork.ShotDuelDistanceMult * Mathf.Lerp(1.4f, 0.7f, resolve) * (1f + (pull * 0.5f))) + (_duelRepeats * 0.5f), Mathf.Max(fork.ShotDuelDistanceMult, fork.ShotDuelDistanceMultMax));

        bool duelKnown = KnowsPosition(enemy, out Vector3 duelPosition);
        _knownThreat = duelKnown ? duelPosition : null;
        _requireHidden = duelKnown && fork.ShotKnownRetreatNeedsHidden;
        bool duelStarted = TryStartRelocating(ScaledRelocateDistance(mult));
        _knownThreat = null;
        _requireHidden = false;
        if (duelStarted)
        {
            _duelCooldownUntil = now + fork.ShotDuelCooldown;
            _duelSeen = 0f;
            _duelHits.Clear();
            _duelForceLeave = false;
            OfferSquadFollow(now, ESquadFollow.Leave, "duel", false);
            _duelRepeats++;
            _lastDuelRetreat = now;
            _longRetreat = true;
            LogEvent($"duel retreat{(forced ? $" ({_duelForceReason})" : "")}: {DuelHas()} for {seen:0} s without hitting it (limit {_duelTarget:0} s), {hits} hits or close bullets on it in {fork.ShotDuelWindow:0} s, player at {distance:0} m, resolve {resolve:0.00}, pull {pull:0.00} ({missed} missed bursts), retreat distance x{mult:0.0} (repeat {_duelRepeats})");
            return;
        }
        _forcedRetreat = false;
        _duelCooldownUntil = now + 30f;
        LogEvent($"duel: wants to leave but has no way out of the player's sight, keeps fighting (player at {distance:0} m)");
    }

    public override void ManualUpdate()
    {
        long started = ShotPerf.Start();
        ManualUpdateCore();
        ShotPerf.Stop(ShotPerf.Update, started);
        ShotPerf.Report(Time.time);
    }

    private void ManualUpdateCore()
    {
        float now = Time.time;
        long t = ShotPerf.Start();
        HookDeath();
        HookEnemyHit();
        ShotPerf.Stop(ShotPerf.Hooks, t);
        t = ShotPerf.Start();
        ProcessCallIn(now);
        ProcessDeferredRadio(now);
        ProcessPendingRogueAlert(now);
        ShotPerf.Stop(ShotPerf.Queues, t);
        ProcessReports(now);
        t = ShotPerf.Start();
        long part = ShotPerf.Start();
        TrackDuelBursts(now);
        ShotPerf.Stop(ShotPerf.Bursts, part);
        part = ShotPerf.Start();
        TickSquadFollow(now);
        ShotPerf.Stop(ShotPerf.SquadFollow, part);
        TryDuelRetreat(now);
        ShotPerf.Stop(ShotPerf.Duel, t);
        t = ShotPerf.Start();
        TryPressureRetreat(now);
        ShotPerf.Stop(ShotPerf.Pressure, t);
        t = ShotPerf.Start();
        TickAreaSuppression(now);
        ShotPerf.Stop(ShotPerf.Suppress, t);
        t = ShotPerf.Start();
        TryProximityInterrupt(now);
        TryOtherEnemyInterrupt(now);
        ShotPerf.Stop(ShotPerf.Interrupts, t);
        t = ShotPerf.Start();
        LogCloseDiagnostics(now);
        ShotPerf.Stop(ShotPerf.CloseDiag, t);
        t = ShotPerf.Start();
        TickSafetyNet(now);
        ShotPerf.Stop(ShotPerf.Tail, t);
    }

    private void TickSafetyNet(float now)
    {
        // Safety net: the decision hook only runs when this shooter is the bot's chosen enemy and nothing earlier in the decision order wins
        // (self actions, grenade avoidance, squad decisions ...). Without this a state could stay set, and keep rules like the sprint override, for good.
        if (Outcome != EShotReaction.None || Relocating)
        {
            if (!Enemy.IsEnemyActive(_enemy))
            {
                if (Relocating)
                {
                    StopRelocating(now, "the shooter is gone");
                }
                if (Outcome != EShotReaction.None)
                {
                    End("the shooter is gone");
                }
            }
            else if (Relocating && now >= _relocateEnd + EXPIRY_GRACE)
            {
                StopRelocating(now, "time limit");
                if (Enabled && !(SeesShooter(_enemy) && _enemy.CanShoot))
                {
                    StartPostRetreat(_enemy, now);
                }
            }
            else if (Outcome != EShotReaction.None && now >= _expireTime + EXPIRY_GRACE)
            {
                // The decision hook never got to make the follow-up pick (a self action, avoid-threat layer etc. was ahead of it): make it now,
                // so the bot does not just stand or drift once its hide is over.
                if (!TryFollowUpOnExpiry(now))
                {
                    End("expired");
                }
            }
        }

        if (now >= _traceUntil || now < _nextTrace)
        {
            return;
        }
        _nextTrace = now + 2f;
        var agent = Bot.BotOwner?.Brain?.Agent;
        Vector3 shotArea = now - _vaguePositionTime < 600f ? _vaguePosition : Bot.Position;
        LogEvent(
            $"trace: layer [{agent?.UsingLayer}] node [{agent?.LastNode}] reason [{agent?.LastReason}], SAIN layer {Bot.ActiveLayer}, enemy {(Bot.GoalEnemy != null)}, "
                + $"moving {Bot.Mover.Moving}, {(Bot.Position - shotArea).magnitude:0} m from the shot area, {(Bot.Position - RelocatePoint).magnitude:0} m from the relocation point"
        );
    }

    /// <summary>
    /// A hide (Cover or Suppress) expired without its follow-up pick and the shooter is still unseen: pick now (fight on / investigate / retreat).
    /// Once per roll. False = nothing to do, the state ends.
    /// </summary>
    private bool TryFollowUpOnExpiry(float now)
    {
        if ((Outcome == EShotReaction.Fight || Outcome == EShotReaction.FightOn) && _enemy != null && !(SeesShooter(_enemy) && _enemy.CanShoot))
        {
            return ChainInvestigation(now);
        }
        if (_followDone || _enemy == null || Outcome != EShotReaction.Cover)
        {
            return false;
        }
        if (SeesShooter(_enemy) && _enemy.CanShoot)
        {
            return false;
        }
        _followDone = true;
        LogEvent("hide expired before the follow-up pick ran, picking now");
        EFollowUp follow = ChooseFollowUp(_enemy);
        if (follow == EFollowUp.Retreat && TryStartRelocating())
        {
            return true;
        }
        if (follow == EFollowUp.Investigate)
        {
            StartInvestigate(now);
        }
        else
        {
            StartFightOn(now);
        }
        return true;
    }

    private void End(string reason)
    {
        LogEvent($"{Outcome} reaction (rolled {_rolled}) over: {reason}");
        if (Outcome == EShotReaction.Hunt && (reason == "ambush over" || reason == "time is up" || reason == "expired") && _enemy != null)
        {
            _returnEnemy = _enemy;
            _returnUntil = Time.time + RETURN_WINDOW;
            LogEvent($"hunt over, still no sight of the shooter: its search runs for the next {RETURN_WINDOW:0} s instead of strolling");
        }
        StartTrace(Time.time);
        SprintWhileInvestigating = false;
        _nadeActive = false;
        _searchUntil = 0f;
        _fireCycleStart = -1f;
        _pendingFollow = null;
        _farFight = false;
        _ambushUntil = 0f;
        _postRetreat = false;
        Bot.Cover.ClearShotState();
        Outcome = EShotReaction.None;
        _hideUntil = 0f;
        _expireTime = 0f;
    }

    private void Roll(Enemy enemy, float distance, bool hit, float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        BotWeaponInfoClass weaponInfo = Bot.Info.WeaponInfo;
        float reach = Mathf.Max(weaponInfo.EffectiveWeaponDistance, 1f);
        // Reach matters more the farther the shooter is: up close every weapon is judged against the same reference reach, and the bot's own
        // reach takes over linearly until the full effect distance.
        float reachWeight = Mathf.Clamp01(distance / Mathf.Max(fork.ShotReachFullEffectDistance, 1f));
        float ratio = Mathf.Lerp(distance / Mathf.Max(fork.ShotReferenceReach, 1f), distance / reach, reachWeight);

        // The zone table is read at the scaled distance; the reach fade below uses the real one.
        float tableRatio = ratio * fork.ShotDistanceScale;
        SampleShares(tableRatio, fork, out float cover, out float fight);

        // Fight fades out beyond the weapon's reach (the sights are already in the reach, which the ratio is relative to).
        fight *= Mathf.Clamp01(Mathf.InverseLerp(FIGHT_FADE_END, FIGHT_FADE_START, ratio));

        // Shooter concealment: harder to place, so less reason to go after it.
        float concealment = 1f;
        if (enemy.EnemyPlayerComponent?.Equipment?.CurrentWeaponInfo?.HasSuppressor == true)
        {
            concealment *= fork.ShotSuppressedShooterFactor;
        }
        if (enemy.EnemyPlayer != null && enemy.EnemyPlayer.IsInPronePose)
        {
            concealment *= fork.ShotProneShooterFactor;
        }
        if (distance <= POINT_BLANK_DISTANCE)
        {
            concealment = 1f;
        }
        fight *= concealment;

        // The less sure the bot is where the shot came from (distance, concealment, weather, light), the less it fights.
        float uncertainty = FreshUncertainty;
        if (fork.ShotUncertaintyHigh > 0f && uncertainty > 0f)
        {
            fight *= Mathf.Lerp(1f, fork.ShotUncertaintyFightMin, Mathf.InverseLerp(0f, fork.ShotUncertaintyHigh, uncertainty));
        }

        // A hit says more about where the shooter is than a bullet passing by.
        if (hit)
        {
            fight *= HIT_INFO_BONUS;
        }

        // Personality.
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                fight *= fork.ShotAggressivePull;
                cover /= fork.ShotAggressivePull;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                cover *= fork.ShotTimidPull;
                fight /= fork.ShotTimidPull;
                break;
        }

        // Bot state: a bot on the move keeps running, a hurt bot hides. Running means less the closer the shooter is: the penalty fades
        // from full at the fade distance to nothing at point blank (fleeing from a close shooter just trades a fight for a hit in the back).
        if (Bot.Mover.Running)
        {
            float strength = Mathf.InverseLerp(POINT_BLANK_DISTANCE, Mathf.Max(fork.ShotRunningFadeDistance, POINT_BLANK_DISTANCE + 1f), distance);
            cover *= Mathf.Lerp(1f, SPRINT_COVER_BONUS, strength);
            fight *= Mathf.Lerp(1f, SPRINT_FIGHT_PENALTY, strength);
        }

        // Facing: a bot already looking toward the shot reacts (fight leans up), a bot with its back turned hides.
        Vector3 shotDirection = (now - _vaguePositionTime < VAGUE_POSITION_MAX_AGE ? _vaguePosition : enemy.EnemyPosition) - Bot.Position;
        float lookAngle = Vector3.Angle(Bot.LookDirection, shotDirection);
        if (lookAngle <= fork.ShotFacingAngle)
        {
            fight *= fork.ShotFacingFightBonus;
        }
        else if (lookAngle >= fork.ShotBackTurnedAngle)
        {
            cover *= fork.ShotBackTurnedCoverBonus;
        }

        // Weapon type counts close up (reach counts far away): short guns and full auto lean to a fight, marksman and sniper rifles and semi-only
        // weapons lean to flight. Full strength up to the weapon effect distance, gone at twice that.
        float weaponStrength = 1f - Mathf.InverseLerp(fork.ShotWeaponEffectDistance, fork.ShotWeaponEffectDistance * 2f, distance);
        if (weaponStrength > 0f)
        {
            float fightMultiplier = 1f;
            float coverMultiplier = 1f;
            switch (weaponInfo.EWeaponClass)
            {
                case EWeaponClass.smg:
                case EWeaponClass.shotgun:
                    fightMultiplier *= fork.ShotWeaponShortGunFightBonus;
                    break;
                case EWeaponClass.marksmanRifle:
                case EWeaponClass.sniperRifle:
                    coverMultiplier *= fork.ShotWeaponLongGunCoverBonus;
                    break;
            }
            if (weaponInfo.HasFireMode(Weapon.EFireMode.fullauto))
            {
                fightMultiplier *= fork.ShotWeaponFullAutoFightBonus;
            }
            else if (weaponInfo.HasFireMode(Weapon.EFireMode.single))
            {
                coverMultiplier *= fork.ShotWeaponSemiOnlyCoverBonus;
            }
            fight *= Mathf.Lerp(1f, fightMultiplier, weaponStrength);
            cover *= Mathf.Lerp(1f, coverMultiplier, weaponStrength);
        }
        ETagStatus health = Bot.Memory.Health.HealthStatus;
        if (health == ETagStatus.BadlyInjured || health == ETagStatus.Dying)
        {
            cover *= INJURED_COVER_BONUS;
            fight *= INJURED_FIGHT_PENALTY;
        }

        NormalizeWithLimits(ref cover, ref fight, fork, tableRatio);

        bool pointBlank = distance <= POINT_BLANK_DISTANCE;
        float pointBlankShare = POINT_BLANK_FIGHT_SHARE;
        if (pointBlank && fight < pointBlankShare)
        {
            // Applied after the limits on purpose: a short-reach weapon would otherwise cap Fight far below this.
            float others = cover;
            float scale = others > 0f ? (1f - pointBlankShare) / others : 0f;
            cover *= scale;
            fight = pointBlankShare;
        }

        // Far range: a Fight draw beyond the start distance may turn into Cover, then a boosted Fight On (never an instant turn and blind fire).
        // The chance ramps from 0 at the start distance to the maximum at the full distance, along a curve.
        float farT = 0f;
        if (fork.ShotFarStartDistance > 0f && fork.ShotFarFightViaCover)
        {
            float u = Mathf.InverseLerp(fork.ShotFarStartDistance, Mathf.Max(fork.ShotFarFullDistance, fork.ShotFarStartDistance + 1f), distance);
            farT = Mathf.Clamp01(fork.ShotFarMaxConvert) * (1f - Mathf.Pow(1f - u, Mathf.Max(fork.ShotFarCurve, 0.1f)));
        }
        string farNote = farT > 0f ? $" (far convert chance {farT:0.00})" : "";

        float pick = Random.value;
        EShotReaction outcome;
        if (pick < cover)
        {
            outcome = EShotReaction.Cover;
        }
        else
        {
            outcome = EShotReaction.Fight;
        }

        bool farFightViaCover = false;
        if (outcome == EShotReaction.Fight && farT > 0f && Random.value < farT)
        {
            outcome = EShotReaction.Cover;
            farFightViaCover = true;
        }

        Apply(enemy, outcome, ratio, now, fork);
        _farFight = farFightViaCover;
        if (farFightViaCover)
        {
            LogEvent($"far fight: {distance:0} m, the Fight roll becomes Cover, then Fight On with weight x{fork.ShotFarFightOnBoost:0.#} (chance {farT:0.00})");
        }
        if (outcome != EShotReaction.Fight)
        {
            // A bot that already sits in cover has nowhere to run to: shot at there, it moves to another cover.
            TrySelfPreservation(hit, now, distance);
        }

        if (fork.ShotReactionLog)
        {
            Logger.LogInfoAlways(
                $"[ShotReaction] t={Time.time:0.0} {Tag} ({Bot.Info.Personality}) vs {enemy.EnemyName}: dist {distance:0} m, reach {reach:0} m, hit {hit}, visible {enemy.IsVisible} ({enemy.VisibleDuration:0.0} s), look angle {lookAngle:0} deg, moving {Bot.Mover.Moving}, running {Bot.Mover.Running}, "
                    + $"weapon {weaponInfo.EWeaponClass} {weaponInfo.SelectedFireMode}{(weaponInfo.HasFireMode(Weapon.EFireMode.fullauto) ? " (full auto available)" : "")}, health {health}, bullets {Bot.BotOwner.WeaponManager?.HaveBullets} reloading {Bot.BotOwner.WeaponManager?.Reload?.Reloading}, "
                    + $"uncertainty {uncertainty:0} m, shares cover {cover:0.00} fight {fight:0.00}{(pointBlank ? " (point blank)" : "")}{farNote} -> {outcome}{(farFightViaCover ? " (Fight via Cover)" : "")}"
            );
        }
    }

    /// <summary>
    /// The zone tables are anchors on a curve over distance / reach: Short holds up to half of the short zone, Mid sits in the middle
    /// of the mid zone, Long holds from LONG_ANCHOR_RATIO. In between the shares are interpolated, so there are no steps at zone edges.
    /// </summary>
    private static void SampleShares(float ratio, ForkSettings fork, out float cover, out float fight)
    {
        float shortAnchor = fork.ShotZoneShort * 0.5f;
        float midAnchor = Mathf.Max((fork.ShotZoneShort + fork.ShotZoneMid) * 0.5f, shortAnchor + 0.01f);
        float longAnchor = Mathf.Max(LONG_ANCHOR_RATIO, midAnchor + 0.01f);

        if (ratio <= shortAnchor)
        {
            cover = fork.ShortCover;
            fight = fork.ShortFight;
        }
        else if (ratio <= midAnchor)
        {
            float t = (ratio - shortAnchor) / (midAnchor - shortAnchor);
            cover = Mathf.Lerp(fork.ShortCover, fork.MidCover, t);
            fight = Mathf.Lerp(fork.ShortFight, fork.MidFight, t);
        }
        else if (ratio <= longAnchor)
        {
            float t = (ratio - midAnchor) / (longAnchor - midAnchor);
            cover = Mathf.Lerp(fork.MidCover, fork.LongCover, t);
            fight = Mathf.Lerp(fork.MidFight, fork.LongFight, t);
        }
        else
        {
            cover = fork.LongCover;
            fight = fork.LongFight;
        }
    }

    /// <summary>
    /// A bot that rolled Suppress may fire at a vague area a bit beyond the normal suppress reach (the roll already faded the chance).
    /// </summary>
    public float SuppressReachStretch(Enemy enemy)
    {
        return IsManaging(enemy) && Outcome == EShotReaction.FightOn ? SUPPRESS_FADE_STRETCH : 1f;
    }

    private static void NormalizeWithLimits(ref float cover, ref float fight, ForkSettings fork, float ratio)
    {
        float total = cover + fight;
        if (total <= 0f)
        {
            cover = 1f;
            fight = 0f;
            return;
        }
        cover /= total;
        fight /= total;

        // Up close the limits relax: Fight may dominate and Cover may drop low. They fade back to the configured values by the Mid anchor.
        float shortAnchor = fork.ShotZoneShort * 0.5f;
        float midAnchor = Mathf.Max((fork.ShotZoneShort + fork.ShotZoneMid) * 0.5f, shortAnchor + 0.01f);
        float closeness = 1f - Mathf.Clamp01(Mathf.InverseLerp(shortAnchor, midAnchor, ratio));
        float maxFight = Mathf.Clamp01(Mathf.Lerp(fork.ShotFightMaxShare, Mathf.Max(fork.ShotFightMaxShare, CLOSE_FIGHT_MAX_SHARE), closeness));
        if (fight > maxFight)
        {
            cover += fight - maxFight;
            fight = maxFight;
        }

        // A strong weapon at a short distance (reach well beyond the shooter) must not hide most of the time: the excess goes to fighting back.
        if (ratio < STRONG_WEAPON_RATIO && cover > STRONG_WEAPON_COVER_MAX)
        {
            fight = Mathf.Min(maxFight, fight + (cover - STRONG_WEAPON_COVER_MAX));
            cover = 1f - fight;
        }

        float minCover = Mathf.Clamp01(Mathf.Lerp(fork.ShotCoverMinShare, Mathf.Min(fork.ShotCoverMinShare, CLOSE_COVER_MIN_SHARE), closeness));
        if (cover < minCover)
        {
            cover = minCover;
            fight = 1f - cover;
        }
    }

    private void Apply(Enemy enemy, EShotReaction outcome, float reachRatio, float now, ForkSettings fork)
    {
        long started = ShotPerf.Start();
        ApplyCore(enemy, outcome, reachRatio, now, fork);
        ShotPerf.Stop(ShotPerf.Apply, started);
    }

    private void ApplyCore(Enemy enemy, EShotReaction outcome, float reachRatio, float now, ForkSettings fork)
    {
        _enemy = enemy;
        Outcome = outcome;
        _rolled = outcome;
        Relocating = false;
        _followDone = false;
        _noCoverDone = false;
        _noCoverSince = -1f;
        _postRetreat = false;
        _repeatRelocations = 0;
        _returnUntil = 0f;
        _ambushUntil = 0f;
        _advanceRunUntil = 0f;
        _advanceRolled = false;
        _nadeActive = false;
        _searchUntil = 0f;
        _fireCycleStart = -1f;
        _pendingFollow = null;
        _farFight = false;
        SprintWhileInvestigating = false;
        _nextRollTime = now + fork.ShotRerollInterval;
        _steerLogged = false;
        LogEvent($"outcome {outcome}");
        _inCoverSince = -1f;

        // Longer hiding the further away the shooter is (relative to the bot's reach).
        float coverTime = Mathf.Lerp(fork.ShotCoverTimeMin, fork.ShotCoverTimeMax, Mathf.Clamp01(reachRatio)) * Random.Range(0.8f, 1.2f);

        // Everything but Fight runs for cover at once, away from the vague shot area where possible.
        Vector3 shotArea = now - _vaguePositionTime < VAGUE_POSITION_MAX_AGE ? _vaguePosition : enemy.EnemyPosition;
        if (outcome != EShotReaction.Fight)
        {
            // Cover prefers cover away from the shot (and flees if there is none, until the no-cover decision).
            Bot.Cover.StartShotCover(shotArea, true);
        }

        switch (outcome)
        {
            case EShotReaction.Fight:
                // Fight back: no hiding, normal combat starts at once.
                _hideUntil = now;
                _expireTime = now + fork.ShotFightDuration;
                StartFightSearch(now, fork);
                break;

            default: // Cover
                _hideUntil = now + coverTime;
                _expireTime = _hideUntil + 0.5f;
                break;
        }
    }

    /// <summary>A Rogue that radioed a hit or bullet this recently has already called it in: its death sends no second alert.</summary>
    private const float ALERT_SENT_WINDOW = 3f;

    private int _reportLoggedCount;
    private float _nextReportAccept;
    private int _bulletStack;
    private float _bulletStackTime = -999f;
    private bool _deathHandled;
    private float _lastAlertSent = -999f;
    private bool _callInPending;
    private bool _callInHit;
    private Enemy _callInEnemy;
    private float _callInDistance;
    private float _callInTime;
    private Vector3? _pendingAlertCenter;
    private bool _pendingForceCover;
    private float _stationaryBlockUntil;
    private float _stayStart = -1f;
    private float _answeringUntil;
    private float _lastTriggerAge;
    private float _nextAreaDiag;
    private float _nextImpactReact;
    private float _lastStayEvent;
    private float _nextStationaryLog;
    private float _lastRogueAlertTime = -999f;
    private bool _pendingSquad;
    private Enemy _pendingAlertEnemy;
    private Vector3 _pendingAlertArea;
    private string _pendingAlertFrom;
    private Vector3 _pendingAlertSource;
    private float _pendingAlertTime;
    private Enemy _enemy;
    private float _noCoverSince = -1f;
    private float _lastNoCoverCall;
    private bool _noCoverDone;
    private int _solidMask = -1;
    private EShotReaction _rolled;
    private float _nextBreakAllowed;
    private float _noHopUntil;
    private float _lastDegradeTime = -999f;
    private float _hideUntil;
    private float _expireTime;
    private float _nextRollTime;
    private float _nextSkipLogTime;
    private bool _steerLogged;
    private float _inCoverSince = -1f;
    private bool _followDone;
    private bool _farFight;
    private float _investigateStart;
    private float _hopHold = 5f;
    private float _nextSelfPreserve;
    private float _nextCloseLog;
    private float _lastShotDistance = 9999f;
    private float _ambushUntil;
    private float _advanceRunUntil;
    private bool _nadeActive;
    private float _searchUntil;
    private float _searchFireUntil;
    private bool _searchRangeLogged;
    private float _fireCycleStart = -1f;
    private float _nadeStart;
    private float _nadeUntil;
    private float _nextNadeTime;
    private EFollowUp? _pendingFollow;
    private bool _advanceRolled;
    private float _traceUntil;
    private float _nextTrace;
    private Enemy _lastSeenEnemy;
    private float _lastSeenTime = -999f;
    private float _relocateEnd;
    private Vector3 _vaguePosition;
    /// <summary>Where the shooter really was when the current rough area was made (the area's own origin).</summary>
    private Vector3 _vagueShooterPosition;
    private Vector3 _lastShooterPosition;
    private float _vagueUncertainty;
    private float _pendingAlertUncertainty;
    private float _vaguePositionTime = -999f;
}
