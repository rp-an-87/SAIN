using System.Collections.Generic;
using EFT;
using SAIN.Components;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
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
    private const float RELOCATE_COVER_HOLD = 1.5f;

    /// <summary>The shooter only counts as seen once it has been continuously visible this long (a flicker of the vision flag must not cancel the reaction).</summary>
    private const float VISIBLE_GRACE = 1f;

    /// <summary>Within this fraction of the bot's reach a visible shooter counts as seen immediately.</summary>
    private const float CLOSE_SIGHT_REACH_FRACTION = 0.4f;

    /// <summary>Once the bot has seen the shooter it keeps counting as seen for this long (a flicker of the vision flag must not restart the reaction).</summary>
    private const float SEEN_HOLD = 4f;

    /// <summary>Within this share of the reach (a strong weapon at a short distance) Cover is capped, the excess goes to fighting back.</summary>
    private const float STRONG_WEAPON_RATIO = 0.5f;
    private const float STRONG_WEAPON_COVER_MAX = 0.6f;

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
    private const float HOP_TIMEOUT = 12f;
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

    public bool IsManaging(Enemy enemy)
    {
        return Outcome != EShotReaction.None && enemy != null && enemy == _enemy;
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
                Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.name}: steering to the {target}, not the shooter");
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

        float radius = distance * fork.ShotPositionErrorPerMeter * (hit ? fork.ShotHitErrorFactor : 1f);
        radius = Mathf.Max(Mathf.Min(radius, fork.ShotPositionErrorMax), fork.ShotPositionErrorMin);
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 point = enemy.EnemyPosition + new Vector3(offset.x, 0f, offset.y);
        if (NavMesh.SamplePosition(point, out NavMeshHit navHit, Mathf.Max(5f, radius * 0.5f), NavMesh.AllAreas))
        {
            point = navHit.position;
        }
        // Never the shooter's exact position: if there is no navmesh near the offset point, the raw offset point is the vague area.
        RememberVaguePosition(point);
        return point;
    }

    private void RememberVaguePosition(Vector3 position)
    {
        _vaguePosition = position;
        _vaguePositionTime = Time.time;
    }

    /// <summary>
    /// The bot was hit by an enemy it cannot see: it learns a vague area, then rolls a reaction.
    /// </summary>
    public void OnHit(Enemy enemy)
    {
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
    /// A shot was fired at the bot (it hit, or a bullet passed close) by an enemy it cannot see.
    /// </summary>
    public void OnShot(Enemy enemy, float distance, bool hit)
    {
        if (!AppliesTo(enemy))
        {
            LogSkipped(enemy, hit);
            return;
        }
        float now = Time.time;
        _safetyBeforeShot = Safety(now, true);
        _lastThreatTime = now;
        _safetyCacheTime = -999f;
        _lastShotDistance = distance;
        // A state is "in effect" until it expires (hide, fight, fight on, investigate) or while the bot retreats. A shot then never starts a new roll.
        bool inEffect = (Outcome != EShotReaction.None && now < _expireTime) || (Relocating && enemy == _enemy);
        if (enemy == _enemy && (inEffect || now < _nextRollTime))
        {
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
    }

    /// <summary>
    /// While the reaction says "hide", the decision is to seek cover. False = let the normal SAIN decisions run.
    /// </summary>
    public bool TryGetDecision(Enemy enemy, out ECombatDecision decision)
    {
        decision = ECombatDecision.None;
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
            return false;
        }
        float now = Time.time;
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
        if (_postRetreat)
        {
            UpdatePostRetreatHold(now);
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
        return false;
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

        bool close = _vaguePositionTime > 0f && (Bot.Position - _vaguePosition).magnitude < INVESTIGATE_STOP_DISTANCE;
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
            if (hopAge <= HOP_TIMEOUT && !stalled)
            {
                decision = ECombatDecision.SeekCover;
                return true;
            }
            // The hop is not getting anywhere (no path, blocked): give it up instead of standing in the open.
            LogEvent($"advance: hop abandoned ({(stalled ? "stalled" : "timed out")} after {hopAge:0.0} s)");
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
            _hopHold = HopHoldTime();
            LogEvent("advance: next cover closer to the shot area");
            decision = ECombatDecision.SeekCover;
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
        if (Outcome != EShotReaction.Hunt || now < _hideUntil || Relocating || now < _nextBreakAllowed)
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
        if (IsCloseRange(distance))
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
        float chance = hit || Bot.Cover.InFoliageCover ? SELF_PRESERVE_HIT_CHANCE : SELF_PRESERVE_BULLET_CHANCE;
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
        if (Random.value > chance)
        {
            return;
        }
        _nextSelfPreserve = now + SELF_PRESERVE_INTERVAL;
        bool inFoliage = Bot.Cover.InFoliageCover;
        if (Bot.Cover.RelocateFromFire(_vaguePosition))
        {
            LogEvent($"self preservation: shot at in {(inFoliage ? "foliage" : "cover")}, moving to another cover (chance {chance:0.00})");
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
        LogEvent($"after hiding: {choice} (weights retreat {retreat:0.00}, fight on {fightOn:0.00}, investigate {investigate:0.00})");
        return choice;
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
        _holdMinUntil = now + POST_RETREAT_MIN_HOLD;
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
        _hopHold = HopHoldTime();
        _expireTime = now + fork.ShotHuntTimeMax;
    }

    /// <summary>
    /// Picks a point on the far side from the vague shot area and starts walking there. False = no usable point, the bot goes back to its normal behavior.
    /// </summary>
    private bool TryStartRelocating(float? distanceOverride = null)
    {
        // A point out of the shooter's line of sight is preferred (the bot is gone, not just further away); any point is the fallback.
        return TryRelocatePass(distanceOverride, true) || TryRelocatePass(distanceOverride, false);
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
        if ((!Relocating && !_postRetreat) || now < _nextRepeatRelocate || _repeatRelocations >= REPEAT_RELOCATE_MAX || IsCloseRange(distance))
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
        if (Random.value > chance)
        {
            return false;
        }
        _nextRepeatRelocate = now + REPEAT_RELOCATE_INTERVAL;
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
        int count = Physics.OverlapSphereNonAlloc(point, DESTINATION_COVER_RADIUS, _destColliders, _destMask);
        float best = 0f;
        for (int i = 0; i < count; i++)
        {
            Collider collider = _destColliders[i];
            if (collider == null)
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

    private bool TryRelocatePass(float? distanceOverride, bool requireBrokenLos)
    {
        ForkSettings fork = GlobalSettings.Fork;
        Vector3 from = Bot.Position;
        Vector3 shotArea = Time.time - _vaguePositionTime < VAGUE_POSITION_MAX_AGE ? _vaguePosition : (_enemy != null ? _enemy.EnemyPosition : from);
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
                Vector3? sampled = NavMeshHelpers.GetNearbyNavMeshPoint(from + (direction * stepDistance), 4f);
                if (sampled == null)
                {
                    continue;
                }
                // Must actually end up further from the shot area than the bot is now.
                if ((sampled.Value - shotArea).magnitude < currentToShot + (stepDistance * 0.5f))
                {
                    continue;
                }
                if (requireBrokenLos && !BreaksLineOfSight(shotArea, sampled.Value))
                {
                    continue;
                }
                float score = ((2 - step) * 0.25f) + DestinationCoverQuality(sampled.Value, shotArea) + Random.Range(0f, 0.6f);
                if (found && score <= bestScore)
                {
                    continue;
                }
                if (!Bot.Mover.CanGoToPoint(sampled.Value, out _, true))
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
            _relocateEnd = Time.time + fork.ShotRelocateTimeMax;
            Outcome = EShotReaction.None;
            _hideUntil = 0f;
            _expireTime = 0f;
            if (fork.ShotReactionLog)
            {
                Logger.LogInfoAlways(
                    $"[ShotReaction] t={Time.time:0.0} {Bot.name}: relocating {(best - from).magnitude:0} m, {(best - shotArea).magnitude:0} m from the shot area"
                );
            }
            BuildRoute(from, best, shotArea);
            return true;
        }
        if (!requireBrokenLos && fork.ShotReactionLog)
        {
            Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.name}: no relocation point found, back to normal");
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
        SprintWhileInvestigating = false;
        Bot.Cover.ClearShotState();
        return true;
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

    private bool ContinueRelocating(Enemy enemy)
    {
        float now = Time.time;
        bool arrived = (Bot.Position - RelocatePoint).sqrMagnitude < RELOCATE_ARRIVED_DISTANCE * RELOCATE_ARRIVED_DISTANCE;
        if (now >= _relocateEnd || arrived || (SeesShooter(enemy) && enemy.CanShoot) || !Enabled)
        {
            string why = now >= _relocateEnd ? "time limit" : arrived ? "arrived" : !Enabled ? "switched off" : "the bot sees the shooter";
            StopRelocating(now, why);
            if (Enabled && (arrived || now >= _relocateEnd) && !(SeesShooter(enemy) && enemy.CanShoot))
            {
                StartPostRetreat(enemy, now);
            }
            return false;
        }
        return true;
    }

    private void StopRelocating(float now, string reason)
    {
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
        Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.name} ({Bot.Info.Profile.WildSpawnType}): {(hit ? "hit" : "bullet")} ignored: {reason}");
    }

    private void LogEvent(string message)
    {
        if (GlobalSettings.Fork.ShotReactionLog)
        {
            Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.name}: {message}");
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

    public override void ManualUpdate()
    {
        float now = Time.time;
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
        float ratio = distance / reach;

        SampleShares(ratio, fork, out float cover, out float fight);

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
        ETagStatus health = Bot.Memory.Health.HealthStatus;
        if (health == ETagStatus.BadlyInjured || health == ETagStatus.Dying)
        {
            cover *= INJURED_COVER_BONUS;
            fight *= INJURED_FIGHT_PENALTY;
        }

        NormalizeWithLimits(ref cover, ref fight, fork, ratio);

        bool pointBlank = distance <= POINT_BLANK_DISTANCE;
        if (pointBlank && fight < POINT_BLANK_FIGHT_SHARE)
        {
            // Applied after the limits on purpose: a short-reach weapon would otherwise cap Fight far below this.
            float others = cover;
            float scale = others > 0f ? (1f - POINT_BLANK_FIGHT_SHARE) / others : 0f;
            cover *= scale;
            fight = POINT_BLANK_FIGHT_SHARE;
        }

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

        Apply(enemy, outcome, ratio, now, fork);
        if (outcome != EShotReaction.Fight)
        {
            // A bot that already sits in cover has nowhere to run to: shot at there, it moves to another cover.
            TrySelfPreservation(hit, now, distance);
        }

        if (fork.ShotReactionLog)
        {
            Logger.LogInfoAlways(
                $"[ShotReaction] t={Time.time:0.0} {Bot.name} ({Bot.Info.Personality}) vs {enemy.EnemyName}: dist {distance:0} m, reach {reach:0} m, hit {hit}, visible {enemy.IsVisible} ({enemy.VisibleDuration:0.0} s), look angle {Vector3.Angle(Bot.LookDirection, enemy.EnemyPosition - Bot.Position):0} deg, moving {Bot.Mover.Moving}, running {Bot.Mover.Running}, "
                    + $"shares cover {cover:0.00} fight {fight:0.00}{(pointBlank ? " (point blank)" : "")} -> {outcome}"
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
                break;

            default: // Cover
                _hideUntil = now + coverTime;
                _expireTime = _hideUntil + 0.5f;
                break;
        }
    }

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
    private float _investigateStart;
    private float _hopHold = 5f;
    private float _nextSelfPreserve;
    private float _nextCloseLog;
    private float _lastShotDistance = 9999f;
    private float _ambushUntil;
    private float _traceUntil;
    private float _nextTrace;
    private Enemy _lastSeenEnemy;
    private float _lastSeenTime = -999f;
    private float _relocateEnd;
    private Vector3 _vaguePosition;
    private float _vaguePositionTime = -999f;
}
