using System;
using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using SAIN.Components;
using SAIN.Components.CoverFinder;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings;
using SAIN.Preset.Shared.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using UnityEngine;

namespace SAIN.SAINComponent.Classes;

public enum CoverFinderState
{
    off = 0,
    on = 1,
}

public enum ECoverSeekingState
{
    None,
    NoCover,
    MoveTo,
    Shift,
    HoldInCover,
}

public class SAINCoverClass : BotComponentClassBase
{
    public ECoverSeekingState CoverSeekingState { get; private set; }

    public event Action OnSpottedInCover;

    public CoverPoint CoverInUse { get; private set; }

    private float _spottedTime;
    public bool SpottedInCover
    {
        get { return _spottedTime > Time.time; }
    }

    public bool HasCover
    {
        get { return CoverInUse != null; }
    }

    public CoverFinderState CurrentCoverFinderState { get; private set; }
    public List<CoverPoint> CoverPoints
    {
        get { return CoverFinder.CoverPoints; }
    }

    public CoverFinderComponent CoverFinder { get; }
    public float LastHitInCoverTime { get; private set; }
    public float TimeSinceLastHitInCover
    {
        get { return Time.time - LastHitInCoverTime; }
    }

    public SAINCoverClass(BotComponent bot)
        : base(bot)
    {
        TickRequirement = ESAINTickState.OnlyNoSleep;
        CoverFinder = bot.GetOrAddComponent<CoverFinderComponent>();
    }

    public override void Init()
    {
        CoverFinder.Init(Bot);
        base.Init();
    }

    public override void ManualUpdate()
    {
        base.ManualUpdate();
        bool active = Bot.HasEnemy;
        ActivateCoverFinder(active);
        if (active)
        {
            createDebug();
        }
    }

    private readonly List<CoverPoint> _coverPoints = [];

    private void LogShotCover(string message)
    {
        if (_shotLogUntil < Time.time || !GlobalSettingsClass.Instance.Fork.ShotReactionLog)
        {
            return;
        }
        Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.ShotReaction.Tag}: {message}");
    }

    private string _lastSprintReason;
    private float _nextNoCoverLogTime;

    private void LogNoCover(string message)
    {
        if (_nextNoCoverLogTime > Time.time)
        {
            return;
        }
        _nextNoCoverLogTime = Time.time + 0.5f;
        LogShotCover(message);
    }

    private const float FLEE_DISTANCE = 25f;
    private const float FLEE_REPATH_INTERVAL = 1f;
    private static readonly float[] FLEE_ANGLES = [0f, 35f, -35f, 70f, -70f];
    private float _nextFleeTime;
    private bool _fleeActive;

    /// <summary>
    /// Fork: shot at by an enemy it cannot place, but its cover finder has no cover points yet (it needs a first scan after the bot
    /// learns of an enemy). Instead of the aggressive dogfight move, the bot sprints away from the vague shot area until cover is found.
    /// </summary>
    private bool TryFleeFromShot()
    {
        if (_shotAwayUntil < Time.time)
        {
            return false;
        }
        if (_nextFleeTime > Time.time)
        {
            return _fleeActive;
        }
        _nextFleeTime = Time.time + FLEE_REPATH_INTERVAL;
        _fleeActive = false;

        Vector3 from = Bot.Position;
        Vector3 away = from - _shotAwayFrom;
        away.y = 0f;
        if (away.sqrMagnitude < 1f)
        {
            away = -Bot.LookDirection;
            away.y = 0f;
        }
        away.Normalize();
        for (int i = 0; i < FLEE_ANGLES.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, FLEE_ANGLES[i], 0f) * away;
            Vector3? point = NavMeshHelpers.GetNearbyNavMeshPoint(from + (direction * FLEE_DISTANCE), 4f);
            if (point == null)
            {
                continue;
            }
            if (Bot.Mover.RunToPoint(point.Value, true, 2f, ESprintUrgency.High))
            {
                _fleeActive = true;
                LogShotCover($"no cover points yet: fleeing {(point.Value - from).magnitude:0} m away from the shot area");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Fork: during a shot reaction a cover point that already has ShotCoverMaxPerPoint other reacting bots at it or on their way to it (within ShotCoverCrowdRadius) is not offered,
    /// so a group does not pile into one spot. False = candidates existed and all were taken (the shot reaction's no-cover decision applies: fight or flight).
    /// </summary>
    private bool filterCrowded()
    {
        var fork = GlobalSettingsClass.Instance.Fork;
        if (fork.ShotCoverMaxPerPoint <= 0 || _shotLogUntil < Time.time || _coverPoints.Count == 0)
        {
            return true;
        }
        BotManagerComponent manager = BotManagerComponent.Instance;
        if (manager?.Bots == null)
        {
            return true;
        }
        float radiusSqr = fork.ShotCoverCrowdRadius * fork.ShotCoverCrowdRadius;
        for (int i = _coverPoints.Count - 1; i >= 0; i--)
        {
            Vector3 position = _coverPoints[i].Position;
            int others = 0;
            foreach (BotComponent other in manager.Bots.Values)
            {
                if (other == null || other == Bot || other.BotOwner == null || other.BotOwner.IsDead || other.ShotReaction == null || !other.ShotReaction.InReaction)
                {
                    continue;
                }
                CoverPoint taken = other.Cover.CoverInUse ?? other.Cover.CoverPoint_MovingTo;
                if (taken != null && (taken.Position - position).sqrMagnitude <= radiusSqr)
                {
                    others++;
                    if (others >= fork.ShotCoverMaxPerPoint)
                    {
                        _coverPoints.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        return _coverPoints.Count > 0;
    }

    private CoverPoint FindCoverPoint()
    {
        if (CoverPoints.Count == 0)
        {
            LogNoCover("cover search: the bot has no cover points at all");
            return null;
        }

        GetGoodCover(CoverPoints, _coverPoints, Bot.Transform.NavData.Position);
        if (_coverPoints.Count == 0)
        {
            LogNoCover($"cover search: none of {CoverPoints.Count} cover points is usable");
            return null;
        }
        if (!filterBushes())
        {
            return null;
        }
        filterForReloadRelocation();
        filterAwayFromShot();
        filterTowardArea();
        if (!filterCrowded())
        {
            LogNoCover("cover search: every candidate is already taken by other reacting bots (no cover for this bot)");
            return null;
        }

        // Fork: right after a shot, with only bushes to choose from, the bot does not hide in one: it counts as "no cover" and the shot reaction
        // decides fight or flight (a full retreat run, or a fight when the shooter is close).
        if (_shotLogUntil >= Time.time && Bot.ShotReaction.NoCoverDecisionPending)
        {
            bool anySolid = false;
            for (int i = 0; i < _coverPoints.Count; i++)
            {
                // A bush far from the shot area and out of its sight is as good as solid cover.
                if (!IsFoliage(_coverPoints[i]) || FoliageQuality(_coverPoints[i].Position, _shotAwayFrom) >= 0.9f)
                {
                    anySolid = true;
                    break;
                }
            }
            if (!anySolid)
            {
                LogNoCover($"cover search: only foliage among {_coverPoints.Count} candidates, no solid cover (fight or flight instead)");
                return null;
            }
        }

        // Foliage (bushes) hides a bot but stops nothing and is a poor place to stay: it only wins when it is FOLIAGE_PATH_PENALTY closer than solid cover.
        _costTemp.Clear();
        for (int i = 0; i < _coverPoints.Count; i++)
        {
            _costTemp[_coverPoints[i]] = CoverCost(_coverPoints[i]);
        }
        _coverPoints.Sort((x, y) => _costTemp[x].CompareTo(_costTemp[y]));
        for (int i = 0; i < _coverPoints.Count; i++)
        {
            CoverPoint coverPoint = _coverPoints[i];
            if (Bot.Mover.GoToCoverPoint(coverPoint, _shallSprint, ESprintUrgency.High))
            {
                LogShotCover(
                    $"cover search: picked candidate {i + 1} of {_coverPoints.Count}, path {coverPoint.PathData.PathLength:0} m, sprint {_shallSprint} ({_lastSprintReason}), "
                        + $"cover '{coverPoint.Collider?.name}' layer {(coverPoint.Collider != null ? LayerMask.LayerToName(coverPoint.Collider.gameObject.layer) : "?")}{(IsFoliage(coverPoint) ? " (foliage)" : "")}{(IsThin(coverPoint) ? " (thin)" : "")}"
                );
                return coverPoint;
            }
        }
        LogNoCover($"cover search: no reachable cover among {_coverPoints.Count} candidates");
        //Logger.LogWarning($"[{Bot.name}] No Cover Point found, even though there are {_coverPoints.Count} cover points available.");
        return null;
    }

    // ---- Combat cover: bushes (Fork Tweaks > Combat Cover) ----

    private static float _seasonBanCheckTime = -10f;
    private static bool _seasonBan;

    /// <summary>Fork: true if this raid's season has its "No Bushes In ..." switch on (foliage thin or gone): no bot uses a bush as cover at all.</summary>
    public static bool BushesBannedBySeason()
    {
        float now = Time.time;
        if (now >= _seasonBanCheckTime && now - _seasonBanCheckTime < 2f)
        {
            return _seasonBan;
        }
        _seasonBanCheckTime = now;
        ISeasonsController controller = Seasons.Controller;
        ESeason season = controller != null ? controller.Season : ESeason.Summer;
        ForkSettings fork = GlobalSettingsClass.Instance.Fork;
        switch (season)
        {
            case ESeason.Autumn:
                _seasonBan = fork.NoBushesAutumn;
                break;
            case ESeason.AutumnLate:
                _seasonBan = fork.NoBushesAutumnLate;
                break;
            case ESeason.Winter:
                _seasonBan = fork.NoBushesWinter;
                break;
            case ESeason.SpringEarly:
                _seasonBan = fork.NoBushesSpringEarly;
                break;
            case ESeason.Spring:
                _seasonBan = fork.NoBushesSpring;
                break;
            default:
                _seasonBan = fork.NoBushesSummer;
                break;
        }
        return _seasonBan;
    }

    /// <summary>Fork: the player has met this bot (the bot saw the player, or was shot at or near by them, within the memory time): a bush gives no surprise any more.</summary>
    private static bool CombatBushBanActive(Enemy enemy)
    {
        ForkSettings fork = GlobalSettingsClass.Instance.Fork;
        if (!fork.CombatBushBan || enemy == null || enemy.IsAI)
        {
            return false;
        }
        float memory = fork.CombatBushMemory;
        if (enemy.Seen && enemy.TimeSinceSeen <= memory)
        {
            return true;
        }
        var status = enemy.Status;
        float now = Time.time;
        return (status.TimeLastShotAtMe >= 0f && now - status.TimeLastShotAtMe <= memory) || (status.TimeLastShotMe >= 0f && now - status.TimeLastShotMe <= memory);
    }

    /// <summary>True if no bush may be used by this bot right now (season ban or combat ban). The view test is per bush, see filterBushes.</summary>
    public bool BushBanNow()
    {
        return BushesBannedBySeason() || CombatBushBanActive(Bot.GoalEnemy);
    }

    private const float BUSH_VIEW_DOT = 0.64f;
    private const float BUSH_VIEW_MAX_DISTANCE = 150f;

    /// <summary>The bush is within about 50 degrees of where the player looks, with no terrain or solid collider in between.</summary>
    private static bool BushInPlayerView(CoverPoint point, Enemy enemy)
    {
        var transform = enemy.EnemyTransform;
        Vector3 to = point.Position + (Vector3.up * 0.5f) - transform.EyePosition;
        float distance = to.magnitude;
        if (distance < 1f)
        {
            return true;
        }
        if (distance > BUSH_VIEW_MAX_DISTANCE || Vector3.Dot(to / distance, transform.LookDirection) < BUSH_VIEW_DOT)
        {
            return false;
        }
        return !BlocksSight(enemy.EnemyPosition, point.Position);
    }

    private float _nextBushLogTime;

    private void LogBushBan(string message)
    {
        if (_nextBushLogTime > Time.time || !GlobalSettingsClass.Instance.Fork.ShotReactionLog)
        {
            return;
        }
        _nextBushLogTime = Time.time + 1f;
        Logger.LogInfoAlways($"[CombatCover] t={Time.time:0.0} {Bot.ShotReaction.Tag}: {message}");
    }

    /// <summary>
    /// Fork: removes the bushes this bot may not pick now from the candidates: all of them in a banned season, all of them once the player has met the bot,
    /// and the ones in the player's view otherwise. False = nothing was left (no cover: the caller returns none).
    /// </summary>
    private bool filterBushes()
    {
        if (_coverPoints.Count == 0)
        {
            return true;
        }
        ForkSettings fork = GlobalSettingsClass.Instance.Fork;
        bool season = BushesBannedBySeason();
        Enemy enemy = Bot.GoalEnemy;
        bool combat = !season && CombatBushBanActive(enemy);
        bool view = !season && !combat && fork.CombatBushBan && fork.CombatBushView && enemy != null && !enemy.IsAI;
        if (!season && !combat && !view)
        {
            return true;
        }
        int removed = 0;
        for (int i = _coverPoints.Count - 1; i >= 0; i--)
        {
            CoverPoint point = _coverPoints[i];
            if (IsFoliage(point) && (season || combat || BushInPlayerView(point, enemy)))
            {
                _coverPoints.RemoveAt(i);
                removed++;
            }
        }
        if (removed == 0)
        {
            return true;
        }
        string why = season ? "season has no foliage" : combat ? "the player has met the bot" : "in the player's view";
        if (_coverPoints.Count == 0)
        {
            LogBushBan($"bushes banned ({why}): {removed} candidate(s) dropped, no cover left (no-cover behaviour instead)");
            return false;
        }
        LogBushBan($"bushes banned ({why}): {removed} bush candidate(s) dropped, {_coverPoints.Count} left");
        return true;
    }

    /// <summary>A cover on the Foliage layer counts as this many metres further away than it is (see FindCoverPoint).</summary>
    private static float FOLIAGE_PATH_PENALTY => GlobalSettingsClass.Instance.Fork.ShotFoliagePenalty;

    private static int _foliageLayer = -1;

    /// <summary>True if the cover point's collider is on the Foliage layer (bushes, hedges): concealment, not protection.</summary>
    public static bool IsFoliage(CoverPoint point)
    {
        Collider collider = point?.Collider;
        if (collider == null)
        {
            return false;
        }
        if (_foliageLayer < 0)
        {
            _foliageLayer = LayerMask.NameToLayer("Foliage");
        }
        return collider.gameObject.layer == _foliageLayer;
    }

    /// <summary>True while the bot sits in (or has been using) foliage cover.</summary>
    public bool InFoliageCover
    {
        get { return IsFoliage(CoverInUse); }
    }

    /// <summary>A bush is a bad place to stay near the shot area, but a fine ambush spot far from it and out of its sight: the penalty fades with that.</summary>
    private static float FOLIAGE_FADE_START => GlobalSettingsClass.Instance.Fork.ShotFoliageFadeStart;
    private static float FOLIAGE_FADE_END => GlobalSettingsClass.Instance.Fork.ShotFoliageFadeEnd;

    private static int _solidSightMask = -1;
    private readonly Dictionary<CoverPoint, float> _costTemp = [];

    /// <summary>True if something solid (terrain, high poly colliders) is between the two points.</summary>
    public static bool BlocksSight(Vector3 shotArea, Vector3 point)
    {
        if (_solidSightMask < 0)
        {
            _solidSightMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("HighPolyCollider"));
        }
        return Physics.Linecast(shotArea + (Vector3.up * 1.5f), point + (Vector3.up * 1.4f), _solidSightMask);
    }

    /// <summary>0 = a bad bush (near the shot area or in its sight), 1 = a good one (far enough and out of sight).</summary>
    public static float FoliageQuality(Vector3 point, Vector3 shotArea)
    {
        float far = Mathf.Clamp01((Vector3.Distance(point, shotArea) - FOLIAGE_FADE_START) / (FOLIAGE_FADE_END - FOLIAGE_FADE_START));
        return far > 0f && BlocksSight(shotArea, point) ? far : 0f;
    }

    private static readonly Dictionary<int, bool> _thinCache = [];
    private static readonly string[] _thinTokens = ["thin", "plastic", "glass", "cardboard", "cloth", "fabric"];

    /// <summary>True if the cover's collider name says it is made of something thin (names like 'Biotoilet_01_Open_BALLISTIC_plastic', 'Wood_box_01_closed_BALLISTIC_Woodthin').</summary>
    public static bool IsThin(CoverPoint point)
    {
        Collider collider = point?.Collider;
        if (collider == null)
        {
            return false;
        }
        int id = collider.GetInstanceID();
        if (_thinCache.TryGetValue(id, out bool thin))
        {
            return thin;
        }
        string name = collider.name != null ? collider.name.ToLowerInvariant() : "";
        thin = false;
        for (int i = 0; i < _thinTokens.Length; i++)
        {
            if (name.Contains(_thinTokens[i]))
            {
                thin = true;
                break;
            }
        }
        _thinCache[id] = thin;
        return thin;
    }

    private float _shotHoldSince;

    private float CoverCost(CoverPoint point)
    {
        float cost = point.PathData.PathLength;
        // Fork: during a shot reaction thin cover (plastic, thin metal or wood, glass) stops nothing: it counts as farther away.
        if (_shotLogUntil >= Time.time && IsThin(point))
        {
            cost += GlobalSettingsClass.Instance.Fork.ShotCoverThinPenalty;
        }
        // Fork: low cover leaves the head exposed (a bot standing behind it is a duck): it counts as farther away during a shot reaction.
        if (_shotLogUntil >= Time.time && point.CoverHeight < GlobalSettingsClass.Instance.Fork.ShotCoverLowHeight)
        {
            cost += GlobalSettingsClass.Instance.Fork.ShotCoverLowPenalty;
        }
        if (IsFoliage(point))
        {
            float quality = _shotLogUntil >= Time.time ? FoliageQuality(point.Position, _shotAwayFrom) : 0f;
            cost += FOLIAGE_PATH_PENALTY * (1f - quality);
        }
        return cost;
    }

    private bool _relocateSolidOnly;

    private const float RELOAD_RELOCATE_ACTIVE_TIME = 10f;
    private const float SHOT_COVER_ACTIVE_TIME = 8f;
    private const float SHOT_COVER_MIN_GAIN = 3f;

    private Vector3 _shotAwayFrom;
    private float _shotAwayUntil;
    private float _shotLogUntil;
    private float _forceSprintUntil;

    /// <summary>
    /// Fork: the bot was shot at by an enemy it cannot place. It runs to cover at once (no walking delay) and prefers cover further
    /// from the vague shot area than it is now. If no such cover exists, the nearest cover is used.
    /// </summary>
    public void StartShotCover(Vector3 shotArea, bool awayFromShot = true)
    {
        _shotAwayFrom = shotArea;
        // Without awayFromShot the nearest cover is used and the bot never flees (it only runs to cover).
        _shotAwayUntil = awayFromShot ? Time.time + SHOT_COVER_ACTIVE_TIME : 0f;
        _shotLogUntil = Time.time + SHOT_COVER_ACTIVE_TIME;
        _forceSprintUntil = Time.time + SHOT_COVER_ACTIVE_TIME;
        _nextFleeTime = 0f;
        _fleeActive = false;
        if (CoverInUse == null)
        {
            CoverPoint_MovingTo = null;
            _timeLastUpdate = 0f;
        }
    }

    private const float ADVANCE_MIN_GAIN = 6f;
    private const float ADVANCE_MAX_PATH = 45f;
    private bool _advanceActive;
    private Vector3 _advanceArea;
    private CoverPoint _advanceFrom;
    private float _holdSince;

    /// <summary>True while the bot runs to its next advance cover point (cover to cover toward the vague shot area).</summary>
    public bool AdvanceActive
    {
        get { return _advanceActive; }
    }

    /// <summary>How long the bot has been in its current cover (0 if it is not in cover).</summary>
    public float TimeHeldInCover
    {
        get { return CoverInUse != null ? Time.time - _holdSince : 0f; }
    }

    public void StopAdvance()
    {
        _advanceActive = false;
        _advanceFrom = null;
    }

    /// <summary>When the current advance hop started (0 if none).</summary>
    public float AdvanceStartTime
    {
        get { return _advanceStart; }
    }

    private float _advanceStart;

    /// <summary>
    /// Fork: the shot reaction is over (or the bot starts something else): drop every flag that made cover selection and sprinting behave
    /// by shot-reaction rules, so normal combat does not keep running under them.
    /// </summary>
    public void ClearShotState()
    {
        _shotAwayUntil = 0f;
        _shotLogUntil = 0f;
        _forceSprintUntil = 0f;
        _relocateUntil = 0f;
        _relocateAvoidPoint = null;
        _relocateSolidOnly = false;
        _fleeActive = false;
        _advanceActive = false;
        _advanceFrom = null;
        _advanceStart = 0f;
    }

    /// <summary>
    /// Fork: one hop of a cover-to-cover advance. Needs a cover point that is clearly closer to the area than the bot is now and not too far to run.
    /// False = no such point (the bot stays where it is). The bot drops its current cover and sprints to the chosen one.
    /// </summary>
    public bool StartAdvanceHop(Vector3 area)
    {
        float botDistance = Vector3.Distance(Bot.Position, area);
        bool any = false;
        foreach (CoverPoint point in CoverPoints)
        {
            if (point != null && !point.CoverData.IsBad && point != CoverInUse && IsAdvancePoint(point, area, botDistance))
            {
                any = true;
                break;
            }
        }
        if (!any)
        {
            return false;
        }
        _shotLogUntil = Time.time + SHOT_COVER_ACTIVE_TIME;
        _advanceActive = true;
        _advanceStart = Time.time;
        _advanceArea = area;
        _advanceFrom = CoverInUse;
        _shotAwayUntil = 0f;
        _forceSprintUntil = Time.time + SHOT_COVER_ACTIVE_TIME;
        CoverInUse = null;
        CoverPoint_MovingTo = null;
        _timeLastUpdate = 0f;
        return true;
    }

    /// <summary>
    /// Fork: self preservation. The bot is being shot at in its cover: it drops that cover and sprints to another one that is not closer to the
    /// shooter and not right next to the old one (the reload relocation filter). False = no other cover point to go to.
    /// </summary>
    public bool RelocateFromFire(Vector3 shotArea)
    {
        if (CoverInUse == null)
        {
            return false;
        }
        float minFromOld = GlobalSettingsClass.Instance.Fork.RelocateMinDistanceFromOldCover;
        float botDistance = Vector3.Distance(Bot.Position, shotArea);
        bool any = false;
        bool anySolid = false;
        bool inFoliage = IsFoliage(CoverInUse);
        bool bushBan = BushBanNow();
        foreach (CoverPoint point in CoverPoints)
        {
            if (
                point != null
                && !point.CoverData.IsBad
                && point != CoverInUse
                && !(bushBan && IsFoliage(point))
                && (point.Position - CoverInUse.Position).sqrMagnitude >= minFromOld * minFromOld
                && Vector3.Distance(point.Position, shotArea) >= botDistance
            )
            {
                any = true;
                if (!IsFoliage(point))
                {
                    anySolid = true;
                    break;
                }
            }
        }
        if (!any)
        {
            return false;
        }
        // Shot at in a bush: it goes for solid cover when there is any (another bush would change nothing).
        _relocateSolidOnly = inFoliage && anySolid;
        _relocateAvoidPoint = CoverInUse;
        _relocateEnemyPos = shotArea;
        _relocateMinEnemyDist = botDistance;
        _relocateUntil = Time.time + SHOT_COVER_ACTIVE_TIME;
        _forceSprintUntil = Time.time + SHOT_COVER_ACTIVE_TIME;
        _shotLogUntil = Time.time + SHOT_COVER_ACTIVE_TIME;
        _shotAwayUntil = 0f;
        _advanceActive = false;
        CoverInUse = null;
        CoverPoint_MovingTo = null;
        _timeLastUpdate = 0f;
        return true;
    }

    private static bool IsAdvancePoint(CoverPoint point, Vector3 area, float botDistance)
    {
        return Vector3.Distance(point.Position, area) <= botDistance - ADVANCE_MIN_GAIN && point.PathData.PathLength <= ADVANCE_MAX_PATH;
    }

    private void filterTowardArea()
    {
        if (!_advanceActive)
        {
            return;
        }
        float botDistance = Vector3.Distance(Bot.Position, _advanceArea);
        _relocateTemp.Clear();
        foreach (CoverPoint point in _coverPoints)
        {
            if (point != _advanceFrom && IsAdvancePoint(point, _advanceArea, botDistance))
            {
                _relocateTemp.Add(point);
            }
        }
        if (_relocateTemp.Count > 0)
        {
            LogShotCover($"advance: {_relocateTemp.Count} of {_coverPoints.Count} cover points are closer to the shot area");
            _coverPoints.Clear();
            _coverPoints.AddRange(_relocateTemp);
        }
        else
        {
            LogShotCover("advance: no cover point closer to the shot area, nearest cover used");
            _advanceActive = false;
        }
    }

    private void filterAwayFromShot()
    {
        if (_shotAwayUntil < Time.time)
        {
            return;
        }
        float botDistance = Vector3.Distance(Bot.Position, _shotAwayFrom);
        _relocateTemp.Clear();
        foreach (CoverPoint point in _coverPoints)
        {
            if (Vector3.Distance(point.Position, _shotAwayFrom) >= botDistance + SHOT_COVER_MIN_GAIN)
            {
                _relocateTemp.Add(point);
            }
        }
        bool log = GlobalSettingsClass.Instance.Fork.ShotReactionLog;
        if (_relocateTemp.Count > 0)
        {
            if (log)
            {
                Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.ShotReaction.Tag}: cover away from the shot, {_relocateTemp.Count} of {_coverPoints.Count} points qualify");
            }
            _coverPoints.Clear();
            _coverPoints.AddRange(_relocateTemp);
        }
        else if (log)
        {
            Logger.LogInfoAlways($"[ShotReaction] t={Time.time:0.0} {Bot.ShotReaction.Tag}: no cover away from the shot, nearest cover of {_coverPoints.Count} used");
        }
    }

    private CoverPoint _relocateAvoidPoint;
    private Vector3 _relocateEnemyPos;
    private float _relocateMinEnemyDist;
    private float _relocateUntil;
    private float _nextRelocateTime;
    private readonly List<CoverPoint> _relocateTemp = [];

    /// <summary>
    /// Called when a bot starts reloading. In close quarters the enemy knows where the bot was shooting from, so reloading
    /// there is a free push for the player. Drop the current cover and pick one further from the enemy's last known position.
    /// </summary>
    public bool TryRelocateForReload(Enemy enemy)
    {
        ForkSettings fork = GlobalSettingsClass.Instance.Fork;
        if (!fork.RelocateEnabled || CoverInUse == null || enemy == null || _nextRelocateTime > Time.time)
        {
            return false;
        }
        Vector3? lastKnown = enemy.LastKnownPosition;
        if (lastKnown == null || enemy.IsVisible || enemy.RealDistance > fork.RelocateMaxEnemyDistance)
        {
            return false;
        }
        bool recent = enemy.Seen && enemy.TimeSinceSeen <= fork.RelocateMaxTimeSinceSeen;
        if (!recent && !enemy.Status.ShotAtMe)
        {
            return false;
        }
        _nextRelocateTime = Time.time + fork.RelocateCooldown;
        if (UnityEngine.Random.value > fork.RelocateChance)
        {
            return false;
        }

        _relocateSolidOnly = false;
        _relocateAvoidPoint = CoverInUse;
        _relocateEnemyPos = lastKnown.Value;
        _relocateMinEnemyDist = Vector3.Distance(Bot.Position, _relocateEnemyPos);
        _relocateUntil = Time.time + RELOAD_RELOCATE_ACTIVE_TIME;
        CoverInUse = null;
        CoverPoint_MovingTo = null;
        _timeLastUpdate = 0f;
        return true;
    }

    private void filterForReloadRelocation()
    {
        if (_relocateAvoidPoint == null)
        {
            return;
        }
        if (_relocateUntil < Time.time)
        {
            _relocateAvoidPoint = null;
            _relocateSolidOnly = false;
            return;
        }
        _relocateTemp.Clear();
        Vector3 oldPos = _relocateAvoidPoint.Position;
        float minFromOld = GlobalSettingsClass.Instance.Fork.RelocateMinDistanceFromOldCover;
        foreach (CoverPoint point in _coverPoints)
        {
            if (point == _relocateAvoidPoint || (_relocateSolidOnly && IsFoliage(point)))
            {
                continue;
            }
            Vector3 pos = point.Position;
            if ((pos - oldPos).sqrMagnitude < minFromOld * minFromOld)
            {
                continue;
            }
            if (Vector3.Distance(pos, _relocateEnemyPos) < _relocateMinEnemyDist)
            {
                continue;
            }
            _relocateTemp.Add(point);
        }
        // nothing suitable: fall back to normal cover selection
        if (_relocateTemp.Count > 0)
        {
            _coverPoints.Clear();
            _coverPoints.AddRange(_relocateTemp);
        }
    }

    /// <summary>
    /// Filter coverpoints, only add good ones to the local list, and calc bot distance to them.
    /// </summary>
    private static void GetGoodCover(List<CoverPoint> coverPoints, List<CoverPoint> localList, Vector3 navPos)
    {
        localList.Clear();
        foreach (CoverPoint point in coverPoints)
        {
            if (point == null || point.CoverData.IsBad)
            {
                continue;
            }

            point.DistanceToBot = point.GetDistance(navPos);
            localList.Add(point);
        }
    }

    private bool CheckStartRun(Enemy enemy, out string reason)
    {
        if (enemy == null || !enemy.EnemyKnown)
        {
            reason = "noEnemy";
            return false;
        }
        if (Bot.Decision.CurrentSelfDecision != ESelfActionType.None)
        {
            reason = "doing self operation";
            return true;
        }
        if (CoverInUse != null)
        {
            reason = "inCover";
            return false;
        }
        if (!Player.MovementContext.CanSprint)
        {
            reason = "cantSprint";
            return false;
        }
        //if (Bot.Cover.CoverPoints.Count == 0)
        //{
        //    reason = "noCoverPoints";
        //    return false;
        //}
        if (enemy.IsSniper && Bot.Info.PersonalitySettings.General.ENEMYSNIPER_ALWAYS_SPRINT_COVER)
        {
            reason = "EnemySniperRun";
            return true;
        }
        if (Time.time < _forceSprintUntil)
        {
            reason = "shotReaction";
            return true;
        }
        if (_startRunCoverTimer < Time.time)
        {
            reason = "timeToRun";
            return true;
        }

        reason = "dontRunYet";
        return false;
    }

    public bool SprintingToCover
    {
        get { return _shallSprint && Bot.Mover.Moving; }
    }

    private bool _shallSprint;

    public void UpdateCover(Enemy enemy)
    {
        if (Time.time - _timeLastUpdate < GetInterval())
        {
            return;
        }

        _timeLastUpdate = Time.time;

        if (BushesBannedBySeason())
        {
            if (IsFoliage(CoverInUse))
            {
                LogBushBan("season has no foliage: leaving the bush it held");
                CoverInUse = null;
            }
            if (IsFoliage(CoverPoint_MovingTo))
            {
                CoverPoint_MovingTo = null;
            }
        }

        _shallSprint = CheckStartRun(enemy, out string reason);
        if (reason != _lastSprintReason)
        {
            _lastSprintReason = reason;
            LogShotCover($"sprint {_shallSprint}: {reason}");
        }

        if (CoverInUse != null)
        {
            // Hold in cover
            //if (!CoverInUse.CoverData.IsBad && CoverPoints.Contains(CoverInUse))
            if (!CoverInUse.CoverData.IsBad)
            {
                Bot.Mover.GoToCoverPoint(CoverInUse, false, ESprintUrgency.High);
                _shallSprint = false;
                SetCoverSeekingState(ECoverSeekingState.HoldInCover);
                return;
            }
            if (Bot.ShotReaction.WideCoverSearch && Time.time - _shotHoldSince < GlobalSettingsClass.Instance.Fork.ShotCoverHold)
            {
                // The next shot moves the guessed shooter position and flags good cover as bad: during a shot reaction the bot keeps its cover a few seconds (no running between props).
                Bot.Mover.GoToCoverPoint(CoverInUse, false, ESprintUrgency.High);
                _shallSprint = false;
                SetCoverSeekingState(ECoverSeekingState.HoldInCover);
                return;
            }
            LogShotCover("cover in use went bad, dropped");
            CoverInUse = null;
        }

        // Move to cover
        if (CoverPoint_MovingTo != null)
        {
            if (_shallSprint)
            {
                Bot.Mover.ActivePath?.RequestStartSprint(ESprintUrgency.High, $"runToCover, {reason}");
            }
            else
            {
                Bot.Mover.ActivePath?.RequestEndSprint(ESprintUrgency.High, $"noRunToCover, {reason}");
            }

            var pathStatus = CoverPoint_MovingTo.PathDistanceStatus;
            var straightStatus = CoverPoint_MovingTo.StraightDistanceStatus;
            if (straightStatus == CoverStatus.InCover || pathStatus == CoverStatus.InCover)
            {
                CoverInUse = CoverPoint_MovingTo;
                _holdSince = Time.time;
                _advanceActive = false;
                CoverPoint_MovingTo = null;
                _shallSprint = false;
                SetCoverSeekingState(ECoverSeekingState.HoldInCover);
                return;
            }
            if (CoverPoint_MovingTo != null && Bot.Mover.GoToCoverPoint(CoverPoint_MovingTo, _shallSprint, ESprintUrgency.High))
            {
                SetCoverSeekingState(ECoverSeekingState.MoveTo);
                return;
            }
            LogShotCover("the move to the chosen cover failed this update (bot may idle)");
        }

        if (CoverPoint_MovingTo == null)
        {
            CoverPoint_MovingTo = FindCoverPoint();
            if (CoverPoint_MovingTo == null)
            {
                // Fork: after a short grace (the finder may still be scanning) the shot reaction decides fight or flight instead of the stopgaps below.
                if (Bot.ShotReaction.HandleNoCover(enemy))
                {
                    SetCoverSeekingState(ECoverSeekingState.NoCover);
                    _shallSprint = false;
                    return;
                }
                if (!TryFleeFromShot())
                {
                    LogNoCover("NO COVER EDGE CASE: no cover points and no flee, falling back to the dogfight move (walks toward the enemy)");
                    const bool AGGRESSIVE_NO_COVER = true;
                    Bot.Mover.DogFight.DogFightMove(AGGRESSIVE_NO_COVER, enemy);
                }
                SetCoverSeekingState(ECoverSeekingState.NoCover);
                _shallSprint = false;
            }
            else
            {
                SetCoverSeekingState(ECoverSeekingState.MoveTo);
            }
        }
    }

    private float GetInterval()
    {
        if (CoverInUse == null && CoverPoint_MovingTo == null)
        {
            return 0.05f;
        }
        if (CoverPoint_MovingTo != null)
        {
            return Bot.Mover.Moving ? 1f : 0.05f;
        }
        if (CoverInUse != null)
        {
            return 0.5f;
        }
        return 0.05f;
    }

    private float _timeLastUpdate;

    private void SetCoverSeekingState(ECoverSeekingState behavior)
    {
        if (CoverSeekingState == behavior)
        {
            return;
        }
        LogShotCover($"cover state {CoverSeekingState} -> {behavior}");
        if (behavior == ECoverSeekingState.HoldInCover)
        {
            _shotHoldSince = Time.time;
        }

        switch (behavior)
        {
            case ECoverSeekingState.Shift:
                throw new NotImplementedException();

            case ECoverSeekingState.MoveTo:
                OnStartMoveToCover();
                break;

            case ECoverSeekingState.None:
                CoverInUse = null;
                CoverPoint_MovingTo = null;
                _shallSprint = false;
                return;

            default:
                break;
        }
        CoverSeekingState = behavior;
    }

    public void StopSeekingCover()
    {
        SetCoverSeekingState(ECoverSeekingState.None);
    }

    private void OnStartMoveToCover()
    {
        _startRunCoverTimer = Time.time + (_runToCoverTime * UnityEngine.Random.Range(_runToCoverTimeRandomMin, _runToCoverTimeRandomMax));
    }

    private const float _runToCoverTime = 1.5f;
    private const float _runToCoverTimeRandomMin = 0.66f;
    private const float _runToCoverTimeRandomMax = 1.33f;
    private float _startRunCoverTimer;

    public CoverPoint CoverPoint_MovingTo { get; private set; }

    private bool CheckMoveToCover(CoverPoint coverPoint, bool sprint)
    {
        if (coverPoint != null && !coverPoint.CoverData.IsBad && Bot.Mover.GoToCoverPoint(coverPoint, sprint, ESprintUrgency.High))
        {
            return true;
        }
        return false;
    }

    public override void Dispose()
    {
        try
        {
            CoverFinder?.Dispose();
        }
        catch { }
        if (_debugCoverObject != null)
        {
            DebugGizmos.DestroyLabel(_debugCoverObject);
            _debugCoverObject = null;
        }
        base.Dispose();
    }

    public CoverPoint FindPointInDirection(Vector3 direction, float dotThreshold = 0.33f, float minDistance = 8f)
    {
        Vector3 botPosition = Bot.Position;
        for (int i = 0; i < CoverPoints.Count; i++)
        {
            CoverPoint point = CoverPoints[i];
            if (point != null && !point.Spotted && !point.CoverData.IsBad)
            {
                Vector3 coverPosition = point.Position;
                Vector3 directionToPoint = botPosition - coverPosition;

                if (
                    directionToPoint.sqrMagnitude > minDistance * minDistance
                    && Vector3.Dot(directionToPoint.normalized, direction.normalized) > dotThreshold
                )
                {
                    return point;
                }
            }
        }
        return null;
    }

    private void createDebug()
    {
#if DEBUG
        if (SAINPlugin.DebugMode && CoverInUse != null)
        {
            DebugGizmos.DrawSphere(CoverInUse.Position, 0.1f, Color.cyan, 0.02f, $"[{Bot.name}] Cover In Use");
            DebugGizmos.DrawLine(CoverInUse.Position, Bot.Position + Vector3.up, Color.cyan, 0.075f, 0.02f, true);
        }
#endif
    }

    public void GetHit(DamageInfo DamageInfo, EBodyPart bodyPart, float floatVal)
    {
        if (CoverInUse != null)
        {
            bool wasSpotted = CoverInUse.Spotted;
            LastHitInCoverTime = Time.time;
            CoverInUse.GetHit(DamageInfo, bodyPart, Bot.GoalEnemy);
            if (CoverInUse.Spotted && !wasSpotted)
            {
                _spottedTime = Time.time + SpottedCoverPoint.SPOTTED_PERIOD;
                OnSpottedInCover?.Invoke();
            }
        }
    }

    public void ActivateCoverFinder(bool value)
    {
        if (value)
        {
            CoverFinder?.LookForCover();
            CurrentCoverFinderState = CoverFinderState.on;
        }
        if (!value)
        {
            CoverFinder?.StopLooking();
            CurrentCoverFinderState = CoverFinderState.off;
        }
    }

    //public void CheckResetCoverInUse()
    //{
    //    if (CoverInUse != null && CoverInUse.CoverData.IsBad)
    //    {
    //        CoverInUse = null;
    //        return;
    //    }
    //
    //    ECombatDecision decision = Bot.Decision.CurrentCombatDecision;
    //    if (decision != ECombatDecision.MoveToCover
    //        && decision != ECombatDecision.RunToCover
    //        && decision != ECombatDecision.Retreat
    //        && decision != ECombatDecision.HoldInCover
    //        && decision != ECombatDecision.ShiftCover)
    //    {
    //        CoverInUse = null;
    //    }
    //}

    public void SortPointsByPathDist()
    {
        CoverFinderComponent.OrderPointsByPathDist(CoverPoints);
    }

    public bool TrySetProneConditional(Enemy enemy)
    {
        var myMoveSettings = Bot.Info.FileSettings.Move;

        bool shallProne =
            myMoveSettings.PRONE_TOGGLE && GlobalSettingsClass.Instance.Move.PRONE_TOGGLE && Bot.Mover.Prone.ShallProneHide(enemy);
        if (
            shallProne
            && (
                Bot.Decision.CurrentSelfDecision != ESelfActionType.None
                || (myMoveSettings.PRONE_SUPPRESS_TOGGLE && Bot.Suppression.IsHeavySuppressed)
            )
        )
        {
            Bot.Mover.Prone.SetProne(true);
            return true;
        }

        //const float minCoverHeightToProne = 0.5f;
        //var point = CoverInUse;
        //if (point != null && shallProne && point.Collider.bounds.size.y < minCoverHeightToProne)
        //{
        //    Bot.Mover.Prone.SetProne(true);
        //    return true;
        //}
        Bot.Mover.Prone.SetProne(false);
        return false;
    }

    public bool CheckLimbsForCover(Enemy enemy)
    {
        if (enemy?.EnemyTransform == null)
        {
            return false;
        }

        var target = enemy.EnemyTransform.WeaponData.WeaponRoot;
        const float rayDistance = 3f;
        if (CheckLimbForCover(BodyPartType.leftLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.leftArm, target, rayDistance))
        {
            return true;
        }
        else if (
            CheckLimbForCover(BodyPartType.rightLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.rightArm, target, rayDistance)
        )
        {
            return true;
        }
        return false;
    }

    private bool CheckLimbForCover(BodyPartType bodyPartType, Vector3 target, float dist = 2f)
    {
        var position = BotOwner.MainParts[bodyPartType].Position;
        Vector3 direction = target - position;
        return Physics.Raycast(position, direction, dist, LayersMaskController.HighPolyWithTerrainMask);
    }

    private DebugLabel _debugCoverObject;
}
