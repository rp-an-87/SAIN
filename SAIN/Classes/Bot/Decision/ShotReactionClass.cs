using EFT;
using SAIN.Components;
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
/// Cover (hide, then forget), Fight (hide for a reaction delay, then normal combat), Suppress (hide and fire bursts),
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

    private static readonly float[] RELOCATE_ANGLES = [0f, 35f, -35f, 70f, -70f, 110f, -110f];
    private const float RELOCATE_ARRIVED_DISTANCE = 3f;
    private const float VAGUE_POSITION_MAX_AGE = 20f;

    public EShotReaction Outcome { get; private set; }

    /// <summary>True while the bot walks away from the area it was shot at (after hiding).</summary>
    public bool Relocating { get; private set; }

    /// <summary>Where the bot is walking to while relocating.</summary>
    public Vector3 RelocatePoint { get; private set; }

    public ShotReactionClass(BotComponent bot)
        : base(bot)
    {
        // Everything is event driven or evaluated when asked.
        CanEverTick = false;
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
        return !enemy.IsVisible;
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
        if (Outcome != EShotReaction.None && enemy == _enemy && now < _nextRollTime)
        {
            // Same fight, too soon for a new roll: the shot only keeps the reaction (and the memory of the shooter) going.
            _forgetAt = Mathf.Max(_forgetAt, now + GlobalSettings.Fork.ShotForgetDelay);
            _expireTime = Mathf.Max(_expireTime, now + 0.5f);
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
        bool seesShooter = enemy.IsVisible && enemy.CanShoot;
        if (
            _relocateWanted
            && !seesShooter
            && now >= _hideUntil
            && (Outcome == EShotReaction.Cover || Outcome == EShotReaction.Suppress)
            && TryStartRelocating()
        )
        {
            decision = ECombatDecision.Relocate;
            return true;
        }
        if (now >= _expireTime || seesShooter)
        {
            // Over, or the bot actually sees the shooter now: normal combat.
            End();
            return false;
        }
        if (now < _hideUntil)
        {
            decision = ECombatDecision.SeekCover;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Picks a point on the far side from the vague shot area and starts walking there. False = no usable point, the bot goes back to its normal behavior.
    /// </summary>
    private bool TryStartRelocating()
    {
        _relocateWanted = false;
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
                        $"[ShotReaction] {Bot.name}: relocating {(sampled.Value - from).magnitude:0} m, {(sampled.Value - shotArea).magnitude:0} m from the shot area"
                    );
                }
                return true;
            }
        }
        if (fork.ShotReactionLog)
        {
            Logger.LogInfoAlways($"[ShotReaction] {Bot.name}: no relocation point found, back to normal");
        }
        return false;
    }

    private bool ContinueRelocating(Enemy enemy)
    {
        float now = Time.time;
        bool arrived = (Bot.Position - RelocatePoint).sqrMagnitude < RELOCATE_ARRIVED_DISTANCE * RELOCATE_ARRIVED_DISTANCE;
        if (now >= _relocateEnd || arrived || (enemy.IsVisible && enemy.CanShoot) || !Enabled)
        {
            StopRelocating(now);
            return false;
        }
        return true;
    }

    private void StopRelocating(float now)
    {
        Relocating = false;
        // Done: the shooter is forgotten (unless new information arrives) and the bot resumes its task from here.
        _forgetAt = now;
        _relocateWanted = false;
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
    /// True when the bot should stop knowing about this shooter: its reaction is over, it was not shot at again, and it never saw it.
    /// </summary>
    public bool ShallForget(Enemy enemy)
    {
        if (_forgetEnemy == null || enemy != _forgetEnemy || !Enabled || Relocating)
        {
            return false;
        }
        if (Time.time < _forgetAt || enemy.IsVisible)
        {
            return false;
        }
        // Information that arrived after the forget time (a new shot, a sighting, a squad report) keeps the bot aware.
        return enemy.KnownPlaces.TimeLastKnownUpdated < _forgetAt;
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
        else if (enemy != null && enemy.IsVisible)
        {
            reason = "the bot can see the shooter";
        }
        else
        {
            reason = "no enemy";
        }
        Logger.LogInfoAlways($"[ShotReaction] {Bot.name} ({Bot.Info.Profile.WildSpawnType}): {(hit ? "hit" : "bullet")} ignored: {reason}");
    }

    private void End()
    {
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

        float cover,
            fight,
            suppress,
            hunt;
        if (ratio < fork.ShotZoneShort)
        {
            cover = fork.ShortCover;
            fight = fork.ShortFight;
            suppress = fork.ShortSuppress;
            hunt = fork.ShortHunt;
        }
        else if (ratio < fork.ShotZoneMid)
        {
            cover = fork.MidCover;
            fight = fork.MidFight;
            suppress = fork.MidSuppress;
            hunt = fork.MidHunt;
        }
        else
        {
            cover = fork.LongCover;
            fight = fork.LongFight;
            suppress = fork.LongSuppress;
            hunt = fork.LongHunt;
        }

        // Weapon: sights pull Fight, class pulls Suppress, reach limits both.
        fight *= Mathf.Clamp(weaponInfo.SightReachMultiplier, 0.5f, 1.6f);
        if (distance > reach * FIGHT_REACH_LIMIT)
        {
            fight = 0f;
        }
        if (!weaponInfo.IsWithinSuppressReach(distance))
        {
            suppress = 0f;
        }
        else
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

        NormalizeWithLimits(ref cover, ref fight, ref suppress, ref hunt, fork);

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
                $"[ShotReaction] {Bot.name} ({Bot.Info.Personality}) vs {enemy.EnemyName}: dist {distance:0} m, reach {reach:0} m, hit {hit}, "
                    + $"shares cover {cover:0.00} fight {fight:0.00} suppress {suppress:0.00} hunt {hunt:0.00} -> {outcome}"
            );
        }
    }

    private static void NormalizeWithLimits(ref float cover, ref float fight, ref float suppress, ref float hunt, ForkSettings fork)
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

        float maxFight = Mathf.Clamp01(fork.ShotFightMaxShare);
        if (fight > maxFight)
        {
            cover += fight - maxFight;
            fight = maxFight;
        }

        float minCover = Mathf.Clamp01(fork.ShotCoverMinShare);
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
        _relocateWanted = fork.ShotRelocateEnabled && (outcome == EShotReaction.Cover || outcome == EShotReaction.Suppress) && Random.value < fork.ShotRelocateChance;
        _nextRollTime = now + fork.ShotRerollInterval;

        // Longer hiding the further away the shooter is (relative to the bot's reach).
        float coverTime = Mathf.Lerp(fork.ShotCoverTimeMin, fork.ShotCoverTimeMax, Mathf.Clamp01(reachRatio)) * Random.Range(0.8f, 1.2f);

        switch (outcome)
        {
            case EShotReaction.Fight:
                _hideUntil = now + Random.Range(Mathf.Min(fork.ShotFightDelayMin, fork.ShotFightDelayMax), Mathf.Max(fork.ShotFightDelayMin, fork.ShotFightDelayMax));
                _expireTime = _hideUntil + fork.ShotFightDuration;
                _forgetAt = _expireTime;
                break;

            case EShotReaction.Hunt:
                _hideUntil = now + Random.Range(Mathf.Min(fork.ShotAssessTimeMin, fork.ShotAssessTimeMax), Mathf.Max(fork.ShotAssessTimeMin, fork.ShotAssessTimeMax));
                _expireTime = now + Mathf.Max(fork.ShotHuntTimeMax, fork.ShotAssessTimeMax);
                _forgetAt = _expireTime;
                break;

            default: // Cover, Suppress
                _hideUntil = now + coverTime;
                _expireTime = _hideUntil + 0.5f;
                _forgetAt = _hideUntil + fork.ShotForgetDelay;
                break;
        }
    }

    private Enemy _enemy;
    private Enemy _forgetEnemy;
    private float _hideUntil;
    private float _expireTime;
    private float _forgetAt;
    private float _nextRollTime;
    private float _nextSkipLogTime;
    private bool _relocateWanted;
    private float _relocateEnd;
    private Vector3 _vaguePosition;
    private float _vaguePositionTime = -999f;
}
