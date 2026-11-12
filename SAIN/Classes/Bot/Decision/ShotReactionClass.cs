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
    Suppress,
    Hunt,
}

/// <summary>
/// Fork: what a bot does when it is shot at by an enemy it cannot see.
/// A hit or a close bullet gives the bot a vague area (never the exact position) and one weighted roll:
/// Cover (hide, then forget), Fight (no hiding: normal combat at once, against the vague area), Suppress (hide and fire bursts),
/// Hunt (hide, assess, then normal search and advance). The roll is event driven: nothing here runs per tick.
/// </summary>
public class ShotReactionClass : BotComponentClassBase
{
    private const float HIT_INFO_BONUS = 1.3f;
    private const float SPRINT_COVER_BONUS = 1.3f;
    private const float SPRINT_FIGHT_PENALTY = 0.5f;
    private const float INJURED_COVER_BONUS = 1.5f;
    private const float INJURED_FIGHT_PENALTY = 0.5f;
    private const float WEAPON_SHORT_RANGE_SUPPRESS = 0.3f;
    private const float WEAPON_MACHINEGUN_SUPPRESS = 1.5f;
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
    private const float INVESTIGATE_STOP_DISTANCE = 15f;
    private const float INVESTIGATE_SOUND_AGE = 1.5f;
    private const float INVESTIGATE_SOUND_GRACE = 2f;

    /// <summary>After stopping, the bot holds cover and watches this long (scaled by personality), then the state ends.</summary>
    private const float AMBUSH_TIME_MIN = 20f;
    private const float AMBUSH_TIME_MAX = 40f;

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
        float radius = distance * fork.ShotPositionErrorPerMeter * (hit ? fork.ShotHitErrorFactor : 1f);
        radius = Mathf.Min(radius, fork.ShotPositionErrorMax);
        if (radius < 1f)
        {
            RememberVaguePosition(estimated);
            return estimated;
        }
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 point = enemy.EnemyPosition + new Vector3(offset.x, 0f, offset.y);
        if (NavMesh.SamplePosition(point, out NavMeshHit navHit, Mathf.Max(5f, radius * 0.5f), NavMesh.AllAreas))
        {
            RememberVaguePosition(navHit.position);
            return navHit.position;
        }
        RememberVaguePosition(estimated);
        return estimated;
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
        // A state is "in effect" until it expires (hide, fight, fight on, investigate) or while the bot retreats. A shot then never starts a new roll.
        bool inEffect = (Outcome != EShotReaction.None && now < _expireTime) || (Relocating && enemy == _forgetEnemy);
        if (enemy == _enemy && (inEffect || now < _nextRollTime))
        {
            // Same fight: the shot only keeps the reaction going.
            string state = Relocating ? "Retreat" : Outcome.ToString();
            if (Outcome != EShotReaction.None)
            {
                _expireTime = Mathf.Max(_expireTime, now + 0.5f);
            }
            LogEvent($"{(hit ? "hit" : "bullet")} at {distance:0} m while {state} is in effect, reaction extended, no new roll");
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
            if (enemy != _forgetEnemy || !ContinueRelocating(enemy))
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
        if (!_followDone && !seesShooter && hiddenLongEnough && (Outcome == EShotReaction.Cover || Outcome == EShotReaction.Suppress))
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
            // Over, or the bot actually sees the shooter now: normal combat.
            End(seesShooter ? "the bot sees the shooter" : "time is up");
            return false;
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
        if (Outcome == EShotReaction.Suppress && _followDone)
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

        SprintWhileInvestigating = true;
        decision = ECombatDecision.Search;
        return true;
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
        if (Outcome == EShotReaction.Suppress)
        {
            retreat *= 0.3f;
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
        Outcome = EShotReaction.Suppress;
        _hideUntil = now;
        _expireTime = now + fork.ShotFightOnDuration;
    }

    /// <summary>Go looking for the shooter: the normal search toward the vague area, until the shooter is seen or the time runs out.</summary>
    private void StartInvestigate(float now)
    {
        ForkSettings fork = GlobalSettings.Fork;
        Outcome = EShotReaction.Hunt;
        _hideUntil = now;
        _investigateStart = now;
        _ambushUntil = 0f;
        _expireTime = now + fork.ShotHuntTimeMax;
    }

    /// <summary>
    /// Picks a point on the far side from the vague shot area and starts walking there. False = no usable point, the bot goes back to its normal behavior.
    /// </summary>
    private bool TryStartRelocating()
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
        float distance = Random.Range(min, max);
        float currentToShot = (from - shotArea).magnitude;

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
                if (!Bot.Mover.CanGoToPoint(sampled.Value, out _, true))
                {
                    continue;
                }
                RelocatePoint = sampled.Value;
                Relocating = true;
                _relocateEnd = Time.time + fork.ShotRelocateTimeMax;
                Outcome = EShotReaction.None;
                _hideUntil = 0f;
                _expireTime = 0f;
                if (fork.ShotReactionLog)
                {
                    Logger.LogInfoAlways(
                        $"[ShotReaction] t={Time.time:0.0} {Bot.name}: relocating {(sampled.Value - from).magnitude:0} m, {(sampled.Value - shotArea).magnitude:0} m from the shot area"
                    );
                }
                return true;
            }
        }
        if (fork.ShotReactionLog)
        {
            Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.name}: no relocation point found, back to normal");
        }
        return false;
    }

    private bool ContinueRelocating(Enemy enemy)
    {
        float now = Time.time;
        bool arrived = (Bot.Position - RelocatePoint).sqrMagnitude < RELOCATE_ARRIVED_DISTANCE * RELOCATE_ARRIVED_DISTANCE;
        if (now >= _relocateEnd || arrived || (SeesShooter(enemy) && enemy.CanShoot) || !Enabled)
        {
            string why = now >= _relocateEnd ? "time limit" : arrived ? "arrived" : !Enabled ? "switched off" : "the bot sees the shooter";
            StopRelocating(now, why);
            return false;
        }
        return true;
    }

    private void StopRelocating(float now, string reason)
    {
        LogEvent($"relocation over: {reason}");
        StartTrace(now);
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
        return Outcome != EShotReaction.Cover;
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

    private void End(string reason)
    {
        LogEvent($"{Outcome} reaction over: {reason}");
        StartTrace(Time.time);
        SprintWhileInvestigating = false;
        _ambushUntil = 0f;
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

        SampleShares(ratio, fork, out float cover, out float fight, out float suppress, out float hunt);

        // Weapon: sights pull Fight, class pulls Suppress, reach fades both out.
        fight *= Mathf.Clamp(weaponInfo.SightReachMultiplier, 0.5f, 1.6f);
        fight *= Mathf.Clamp01(Mathf.InverseLerp(FIGHT_FADE_END, FIGHT_FADE_START, ratio));
        float suppressFade = 1f;
        if (fork.SuppressLimitEnabled && Bot.Info.Profile.RangeEnabled)
        {
            float suppressStart = fork.SuppressReachFraction;
            suppressFade = Mathf.Clamp01(Mathf.InverseLerp(suppressStart * SUPPRESS_FADE_STRETCH, suppressStart, ratio));
        }
        suppress *= suppressFade;
        if (suppress > 0f)
        {
            switch (weaponInfo.EWeaponClass)
            {
                case EWeaponClass.machinegun:
                    suppress *= WEAPON_MACHINEGUN_SUPPRESS;
                    break;
                case EWeaponClass.smg:
                case EWeaponClass.pistol:
                case EWeaponClass.shotgun:
                    suppress *= WEAPON_SHORT_RANGE_SUPPRESS;
                    break;
            }
        }

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
        fight *= concealment;
        suppress *= concealment;
        hunt *= concealment;

        // A hit says more about where the shooter is than a bullet passing by.
        if (hit)
        {
            fight *= HIT_INFO_BONUS;
            hunt *= HIT_INFO_BONUS;
        }

        // Personality.
        switch (Bot.Info.Personality)
        {
            case EPersonality.Wreckless:
            case EPersonality.GigaChad:
            case EPersonality.Chad:
                fight *= fork.ShotAggressivePull;
                suppress *= fork.ShotAggressivePull;
                hunt *= fork.ShotAggressivePull;
                cover /= fork.ShotAggressivePull;
                break;
            case EPersonality.Rat:
            case EPersonality.Timmy:
            case EPersonality.Coward:
                cover *= fork.ShotTimidPull;
                fight /= fork.ShotTimidPull;
                suppress /= fork.ShotTimidPull;
                hunt /= fork.ShotTimidPull;
                break;
        }

        // Bot state: a bot on the move keeps running, a hurt bot hides.
        if (Bot.Mover.Running)
        {
            cover *= SPRINT_COVER_BONUS;
            fight *= SPRINT_FIGHT_PENALTY;
        }
        ETagStatus health = Bot.Memory.Health.HealthStatus;
        if (health == ETagStatus.BadlyInjured || health == ETagStatus.Dying)
        {
            cover *= INJURED_COVER_BONUS;
            fight *= INJURED_FIGHT_PENALTY;
        }

        NormalizeWithLimits(ref cover, ref fight, ref suppress, ref hunt, fork, ratio);

        float pick = Random.value;
        EShotReaction outcome;
        if (pick < cover)
        {
            outcome = EShotReaction.Cover;
        }
        else if (pick < cover + fight)
        {
            outcome = EShotReaction.Fight;
        }
        else if (pick < cover + fight + suppress)
        {
            outcome = EShotReaction.Suppress;
        }
        else
        {
            outcome = EShotReaction.Hunt;
        }

        Apply(enemy, outcome, ratio, now, fork);

        if (fork.ShotReactionLog)
        {
            Logger.LogInfoAlways(
                $"[ShotReaction] t={Time.time:0.0} {Bot.name} ({Bot.Info.Personality}) vs {enemy.EnemyName}: dist {distance:0} m, reach {reach:0} m, hit {hit}, visible {enemy.IsVisible} ({enemy.VisibleDuration:0.0} s), look angle {Vector3.Angle(Bot.LookDirection, enemy.EnemyPosition - Bot.Position):0} deg, moving {Bot.Mover.Moving}, running {Bot.Mover.Running}, "
                    + $"shares cover {cover:0.00} fight {fight:0.00} suppress {suppress:0.00} hunt {hunt:0.00} -> {outcome}"
            );
        }
    }

    /// <summary>
    /// The zone tables are anchors on a curve over distance / reach: Short holds up to half of the short zone, Mid sits in the middle
    /// of the mid zone, Long holds from LONG_ANCHOR_RATIO. In between the shares are interpolated, so there are no steps at zone edges.
    /// </summary>
    private static void SampleShares(float ratio, ForkSettings fork, out float cover, out float fight, out float suppress, out float hunt)
    {
        float shortAnchor = fork.ShotZoneShort * 0.5f;
        float midAnchor = Mathf.Max((fork.ShotZoneShort + fork.ShotZoneMid) * 0.5f, shortAnchor + 0.01f);
        float longAnchor = Mathf.Max(LONG_ANCHOR_RATIO, midAnchor + 0.01f);

        if (ratio <= shortAnchor)
        {
            cover = fork.ShortCover;
            fight = fork.ShortFight;
            suppress = fork.ShortSuppress;
            hunt = fork.ShortHunt;
        }
        else if (ratio <= midAnchor)
        {
            float t = (ratio - shortAnchor) / (midAnchor - shortAnchor);
            cover = Mathf.Lerp(fork.ShortCover, fork.MidCover, t);
            fight = Mathf.Lerp(fork.ShortFight, fork.MidFight, t);
            suppress = Mathf.Lerp(fork.ShortSuppress, fork.MidSuppress, t);
            hunt = Mathf.Lerp(fork.ShortHunt, fork.MidHunt, t);
        }
        else if (ratio <= longAnchor)
        {
            float t = (ratio - midAnchor) / (longAnchor - midAnchor);
            cover = Mathf.Lerp(fork.MidCover, fork.LongCover, t);
            fight = Mathf.Lerp(fork.MidFight, fork.LongFight, t);
            suppress = Mathf.Lerp(fork.MidSuppress, fork.LongSuppress, t);
            hunt = Mathf.Lerp(fork.MidHunt, fork.LongHunt, t);
        }
        else
        {
            cover = fork.LongCover;
            fight = fork.LongFight;
            suppress = fork.LongSuppress;
            hunt = fork.LongHunt;
        }
    }

    /// <summary>
    /// A bot that rolled Suppress may fire at a vague area a bit beyond the normal suppress reach (the roll already faded the chance).
    /// </summary>
    public float SuppressReachStretch(Enemy enemy)
    {
        return IsManaging(enemy) && Outcome == EShotReaction.Suppress ? SUPPRESS_FADE_STRETCH : 1f;
    }

    private static void NormalizeWithLimits(ref float cover, ref float fight, ref float suppress, ref float hunt, ForkSettings fork, float ratio)
    {
        float total = cover + fight + suppress + hunt;
        if (total <= 0f)
        {
            cover = 1f;
            fight = suppress = hunt = 0f;
            return;
        }
        cover /= total;
        fight /= total;
        suppress /= total;
        hunt /= total;

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
            float excess = cover - STRONG_WEAPON_COVER_MAX;
            cover = STRONG_WEAPON_COVER_MAX;
            float rest = fight + suppress + hunt;
            if (rest > 0f)
            {
                float factor = 1f + (excess / rest);
                fight *= factor;
                suppress *= factor;
                hunt *= factor;
            }
            else
            {
                fight += excess;
            }
        }

        float minCover = Mathf.Clamp01(Mathf.Lerp(fork.ShotCoverMinShare, Mathf.Min(fork.ShotCoverMinShare, CLOSE_COVER_MIN_SHARE), closeness));
        if (cover < minCover)
        {
            float others = fight + suppress + hunt;
            float need = minCover - cover;
            float scale = others > 0f ? Mathf.Max(0f, (others - need) / others) : 0f;
            fight *= scale;
            suppress *= scale;
            hunt *= scale;
            cover = 1f - fight - suppress - hunt;
        }
    }

    private void Apply(Enemy enemy, EShotReaction outcome, float reachRatio, float now, ForkSettings fork)
    {
        _enemy = enemy;
        _forgetEnemy = enemy;
        Outcome = outcome;
        Relocating = false;
        _followDone = false;
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
            // Suppress and Hunt take the nearest cover and never flee (they fight or advance, with no cover points SAIN's own advance runs);
            // Cover prefers cover away from the shot.
            Bot.Cover.StartShotCover(shotArea, outcome == EShotReaction.Cover);
        }
        if (outcome == EShotReaction.Suppress)
        {
            coverTime = fork.ShotCoverTimeMin * Random.Range(0.8f, 1.2f);
        }

        switch (outcome)
        {
            case EShotReaction.Fight:
                // Fight back: no hiding, normal combat starts at once.
                _hideUntil = now;
                _expireTime = now + fork.ShotFightDuration;
                break;

            case EShotReaction.Hunt:
                _hideUntil = now + Random.Range(Mathf.Min(fork.ShotAssessTimeMin, fork.ShotAssessTimeMax), Mathf.Max(fork.ShotAssessTimeMin, fork.ShotAssessTimeMax));
                _investigateStart = _hideUntil;
                _expireTime = now + Mathf.Max(fork.ShotHuntTimeMax, fork.ShotAssessTimeMax);
                break;

            default: // Cover, Suppress
                _hideUntil = now + coverTime;
                _expireTime = _hideUntil + 0.5f;
                break;
        }
    }

    private Enemy _enemy;
    private Enemy _forgetEnemy;
    private float _hideUntil;
    private float _expireTime;
    private float _nextRollTime;
    private float _nextSkipLogTime;
    private bool _steerLogged;
    private float _inCoverSince = -1f;
    private bool _followDone;
    private float _investigateStart;
    private float _ambushUntil;
    private float _traceUntil;
    private float _nextTrace;
    private Enemy _lastSeenEnemy;
    private float _lastSeenTime = -999f;
    private float _relocateEnd;
    private Vector3 _vaguePosition;
    private float _vaguePositionTime = -999f;
}
