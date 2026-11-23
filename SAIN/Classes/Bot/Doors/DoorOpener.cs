using System.Collections.Generic;
using EFT;
using EFT.Interactive;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.Shared.GlobalSettings;
using SAIN.Preset.Shared.GlobalSettings.Categories;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover;

public class DoorOpener : BotComponentClassBase
{
    public bool Interacting { get; private set; }

    public EInteractionType InteractionType { get; private set; }

    /// <summary>
    /// Fork: set when a pulled-open door will swing into the bot. The bot walks here (outside the swing circle) during the interaction.
    /// </summary>
    public bool HasRetreatPoint { get; private set; }

    public Vector3 RetreatPoint { get; private set; }

    /// <summary>
    /// Fork: true once after an interaction ends, so the path can be recalculated around the now-open door.
    /// </summary>
    public bool ConsumeRecalcRequest()
    {
        bool value = _recalcAfterDoor;
        _recalcAfterDoor = false;
        return value;
    }

    private bool _recalcAfterDoor;

    public DoorOpener(BotComponent sain)
        : base(sain)
    {
        TickRequirement = ESAINTickState.OnlyNoSleep;
    }

    private const float DOOR_UPDATE_INTERVAL = 0.5f;

    private List<DoorDataStruct> _interactionDoors { get; } = [];
    private List<DoorDataStruct> _allDoors { get; } = [];
    public NavGraphVoxelSimple CurrentVoxel { get; private set; }

    public bool TryInteractWithDoor(EInteractionType interactionType, float time, DoorDataStruct data)
    {
        if (!InteractWithDoor(ref data, interactionType))
        {
#if DEBUG
            Logger.LogDebug($"[{Bot.name}]:[{data.Door.Id}] failed to interact with door");
#endif
            Clear();
            return false;
        }
        _interactionDoors[_interactionDoorIndex] = data;
        // Fork: cooldown timestamps must live in _allDoors, _interactionDoors is rebuilt from it on every update.
        for (int i = 0; i < _allDoors.Count; i++)
        {
            if (_allDoors[i].Id == data.Id)
            {
                _allDoors[i] = data;
                break;
            }
        }
        Interacting = true;
        ActiveDoor = data;
        InteractionType = interactionType;
        HasRetreatPoint = false;
        ForkSettings fork = GlobalSettingsClass.Instance.Fork;
        if (fork.DoorRetreatEnabled && interactionType == EInteractionType.Open && IsDoorPullOpen(data, Bot.NavMeshPosition))
        {
            HasRetreatPoint = TryGetRetreatPoint(data, out Vector3 retreat);
            RetreatPoint = retreat;
        }
        bool pull = IsDoorPullOpen(data, Bot.NavMeshPosition);
        float duration = pull ? 1.25f : 1f;
        if (HasRetreatPoint)
        {
            // Fork: stay in the interaction until the bot has had time to walk out of the swing circle.
            Vector3 toRetreat = RetreatPoint - Bot.Position;
            toRetreat.y = 0f;
            duration = Mathf.Clamp((toRetreat.magnitude / Mathf.Max(0.05f, fork.DoorRetreatSpeed)) + 0.5f, duration, fork.DoorInteractionMaxTime);
        }
        _doorInteractionEndTime = time + duration;
        Logger.LogInfo(
            $"[DoorFork] {Bot.name} door:{data.Door.Id} {interactionType} pull:{pull} retreat:{HasRetreatPoint} "
                + $"leaf:{(pull ? _dbgLeafLength : 0f):0.00} hingeDist:{(pull ? _dbgHingeDistance : 0f):0.00} hold:{duration:0.00}"
        );
        Bot.Player.MovementContext.IgnoreInteractionCollision(data.Door.Collider, true);
        return true;
    }

    /// <summary>
    /// Fork: last resort for a bot that is stuck. Closes the nearest open door within reach, which frees a bot wedged against the open leaf.
    /// Only called when the bot has stopped making progress, so it is not part of normal door handling.
    /// </summary>
    public bool TryCloseBlockingDoor(float time)
    {
        if (Interacting)
        {
            return false;
        }
        ForkSettings fork = GlobalSettingsClass.Instance.Fork;
        if (!fork.StuckCloseDoor)
        {
            return false;
        }

        int best = -1;
        float bestSqr = fork.StuckCloseDoorRange * fork.StuckCloseDoorRange;
        for (int i = 0; i < _allDoors.Count; i++)
        {
            DoorDataStruct door = _allDoors[i];
            if (door.Door == null || door.Door.DoorState != EDoorState.Open || !door.CanInteractByTime(time))
            {
                continue;
            }
            if (door.CurrentSqrMagnitude < bestSqr)
            {
                bestSqr = door.CurrentSqrMagnitude;
                best = i;
            }
        }
        if (best < 0)
        {
            return false;
        }

        // TryInteractWithDoor writes back into _interactionDoors at _interactionDoorIndex, so this door has to be its only entry.
        _interactionDoors.Clear();
        _interactionDoors.Add(_allDoors[best]);
        _interactionDoorIndex = 0;
        Logger.LogInfo($"[DoorFork] {Bot.name} stuck near open door:{_interactionDoors[0].Id}, closing it");
        return TryInteractWithDoor(EInteractionType.Close, time, _interactionDoors[0]);
    }

    public DoorDataStruct GetActiveDoor()
    {
        if (_interactionDoors.Count > 0 && _interactionDoorIndex < _interactionDoors.Count)
        {
            return _interactionDoors[_interactionDoorIndex];
        }
        return ActiveDoor;
    }

    public bool SelectDoor(out EInteractionType interactionType, out DoorDataStruct currentDoor, IBotPathData pathData)
    {
        bool selected = SelectDoorCore(out interactionType, out currentDoor, pathData);
        if (!selected)
        {
            LogMissedDoor();
        }
        return selected;
    }

    // ---- Fork: fast bots (rush, push) ----
    // A sprinting bot covers 3 m in about half a second, so a door list refreshed every 0.5 s showed it the door when it was already at it. The distances are now
    // refreshed every tick while the bot is fast, the interaction cast reaches further with speed, and the path mover ends the sprint when a shut door is ahead.

    private const float FAST_SPEED = 2.5f;
    private const float BRAKE_BASE = 1.5f;
    private const float BRAKE_PER_SPEED = 0.7f;
    private const float BRAKE_MAX = 7f;
    private const float BRAKE_MIN_DOT = 0.7f;
    private const float MISSED_DOOR_DISTANCE_SQR = 1f;
    private float _nextMissedLogTime;

    private float FlatSpeed()
    {
        Vector3 velocity = Bot.Player.MovementContext.Velocity;
        velocity.y = 0f;
        return velocity.magnitude;
    }

    /// <summary>
    /// True if the bot is fast and a shut door it can open lies ahead within braking range: the path mover ends the sprint so the bot arrives at walking pace.
    /// Uses the door distances SelectDoor refreshed this tick.
    /// </summary>
    public bool ShallBrakeForDoor(Vector3 moveDirectionNormal)
    {
        if (Interacting)
        {
            return false;
        }
        float speed = FlatSpeed();
        if (speed < FAST_SPEED)
        {
            return false;
        }
        float brake = Mathf.Min(BRAKE_MAX, BRAKE_BASE + (speed * BRAKE_PER_SPEED));
        float brakeSqr = brake * brake;
        float time = Time.time;
        for (int i = 0; i < _allDoors.Count; i++)
        {
            DoorDataStruct door = _allDoors[i];
            if (door.Door == null || door.Door.DoorState != EDoorState.Shut || !door.CanInteractByTime(time))
            {
                continue;
            }
            if (door.CurrentSqrMagnitude > brakeSqr || Vector3.Dot(door.DirectionNormal, moveDirectionNormal) < BRAKE_MIN_DOT)
            {
                continue;
            }
            return true;
        }
        return false;
    }

    /// <summary>Diagnostic: a fast bot is within 1 m of a shut door and no interaction started. Rate limited.</summary>
    private void LogMissedDoor()
    {
        if (Interacting || _nextMissedLogTime > Time.time)
        {
            return;
        }
        float speed = FlatSpeed();
        if (speed < FAST_SPEED)
        {
            return;
        }
        float time = Time.time;
        for (int i = 0; i < _allDoors.Count; i++)
        {
            DoorDataStruct door = _allDoors[i];
            if (door.Door == null || door.Door.DoorState != EDoorState.Shut || door.CurrentSqrMagnitude > MISSED_DOOR_DISTANCE_SQR)
            {
                continue;
            }
            _nextMissedLogTime = time + 2f;
            Logger.LogInfo(
                $"[DoorFork] {Bot.name} missed door:{door.Id} dist:{Mathf.Sqrt(door.CurrentSqrMagnitude):0.0} speed:{speed:0.0} "
                    + $"sprint:{Bot.Player.IsSprintEnabled} cooldown:{!door.CanInteractByTime(time)}"
            );
            return;
        }
    }

    private bool SelectDoorCore(out EInteractionType interactionType, out DoorDataStruct currentDoor, IBotPathData pathData)
    {
        const float RAY_LENGTH = 3f;
        Vector3 botPosition = Bot.Position;
        float time = Time.time;

        if (Interacting)
        {
            if (_doorInteractionEndTime < time)
            {
                Clear();
                _recalcAfterDoor = true;

                interactionType = EInteractionType.Open; // Default to open if interaction is done
                currentDoor = default;
                return false;
            }
            else
            {
                interactionType = InteractionType;
                currentDoor = _interactionDoors[_interactionDoorIndex];
                return true;
            }
        }

        SearchForDoors(botPosition, time);

        if (_interactionDoors.Count == 0)
        {
            interactionType = EInteractionType.Open; // Default to open if no doors found
            currentDoor = ActiveDoor;
            return false;
        }

        CornerMoveData moveData = pathData.CurrentCornerMoveData;
        Ray ray = new() { origin = botPosition + Vector3.up, direction = moveData.CornerDirectionFromBotNormal * RAY_LENGTH };

        DoorDataStruct data = ActiveDoor;
        // Fork: closing is opt-in. A door that is already open is left alone, otherwise the bot closes the door it just opened.
        bool allowClose = GlobalSettingsClass.Instance.Fork.BotsCloseDoors;
        float castDistance = Mathf.Max(1.5f, (FlatSpeed() * 0.3f) + 0.9f);
        if (!RaycastToDoors(out interactionType, ref data, out int index, RAY_LENGTH, ray, _interactionDoors, allowClose, castDistance))
        {
            currentDoor = ActiveDoor;
            return false;
        }
        _interactionDoorIndex = index;
        _interactionDoors[index] = data;
        //bool value = TryInteractWithDoor(interactionType, time, ref data);
        currentDoor = data;
        return true;
    }

    private void Clear()
    {
        Bot.Player.MovementContext.IgnoreInteractionCollision(ActiveDoor.Door?.Collider, false);
        Interacting = false;
        _interactionDoorIndex = 0;
        ActiveDoor = new();
        _doorInteractionEndTime = 0;
        InteractionType = EInteractionType.Open;
        HasRetreatPoint = false;
    }

    private float _dbgLeafLength;
    private float _dbgHingeDistance;

    /// <summary>
    /// A door swings around its hinge, so the area it sweeps is a circle with the leaf length as radius.
    /// Finds a point just outside that circle, in the direction away from the hinge. Runs once per door interaction.
    /// </summary>
    private bool TryGetRetreatPoint(DoorDataStruct data, out Vector3 point)
    {
        point = default;
        NavMeshDoorLink link = data.Link;
        Vector3 close1 = link.Close1;
        Vector3 close2 = link.Close2_Normal;
        // The hinge is the end of the closed leaf that sits next to an end of the open leaf.
        float sqr1 = Mathf.Min(FlatSqrDist(close1, link.Open1), FlatSqrDist(close1, link.Open2));
        float sqr2 = Mathf.Min(FlatSqrDist(close2, link.Open1), FlatSqrDist(close2, link.Open2));
        Vector3 hinge = sqr1 <= sqr2 ? close1 : close2;
        Vector3 leafEnd = sqr1 <= sqr2 ? close2 : close1;
        float leafLength = Mathf.Sqrt(FlatSqrDist(hinge, leafEnd));

        Vector3 botPosition = Bot.NavMeshPosition;
        Vector3 away = botPosition - hinge;
        away.y = 0f;
        float distance = away.magnitude;
        _dbgLeafLength = leafLength;
        _dbgHingeDistance = distance;
        ForkSettings fork = GlobalSettingsClass.Instance.Fork;
        float needed = leafLength + fork.DoorRetreatMargin;
        if (distance < 0.01f || distance >= needed)
        {
            return false;
        }

        float move = Mathf.Min(needed - distance, fork.DoorRetreatMaxDistance);
        Vector3 target = botPosition + (away / distance) * move;
        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 0.75f, NavMesh.AllAreas))
        {
            return false;
        }
        point = hit.position;
        return true;
    }

    private static float FlatSqrDist(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return (x * x) + (z * z);
    }

    private int _interactionDoorIndex;

    private void SearchForDoors(Vector3 botPosition, float time)
    {
        // Fork: a fast bot refreshes the distances every tick and looks for a new voxel every 0.2 s.
        bool fast = FlatSpeed() >= FAST_SPEED;
        bool voxelTick = _nextDoorUpdateTime < time;
        if (voxelTick || fast)
        {
            if (voxelTick)
            {
                _nextDoorUpdateTime = time + (fast ? 0.2f : DOOR_UPDATE_INTERVAL);
                BotOwner.AIData.SetPosToVoxel(botPosition);

                var lastVoxel = CurrentVoxel;
                CurrentVoxel = BotOwner.VoxelesPersonalData.CurVoxel;

                if (lastVoxel != CurrentVoxel)
                {
                    _allDoors.Clear();
                    if (CurrentVoxel != null)
                    {
                        foreach (var link in CurrentVoxel.DoorLinks)
                        {
                            if (IsDoorOpenable(link.Door))
                            {
                                _allDoors.Add(new DoorDataStruct(link));
                            }
                        }
                    }
                }
            }

            _interactionDoors.Clear();
            for (int i = 0; i < _allDoors.Count; i++)
            {
                DoorDataStruct door = _allDoors[i];
                door.ManualUpdate(botPosition);
                if (door.InRangeToInteract(door.Door) && door.CanInteractByTime(time))
                {
                    _interactionDoors.Add(door);
                }
                _allDoors[i] = door;
            }
        }
    }

    private static bool FindDoorFromCollider(Collider collider, out DoorDataStruct data, out int index, List<DoorDataStruct> doors)
    {
        if (collider == null)
        {
            data = default;
            index = -1;
            return false;
        }
        data = default;
        for (index = 0; index < doors.Count; index++)
        {
            data = doors[index];
            if (data.Door.Collider == collider)
            {
                return true;
            }
            if (collider.GetComponent<NavMeshDoorLink>() == data.Link)
            {
                Logger.LogDebug($"Found NavMeshDoorLink from collider");
                return true;
            }
        }
        return false;
    }

    private static bool RaycastToDoors(
        out EInteractionType interactionType,
        ref DoorDataStruct data,
        out int index,
        float RAY_LENGTH,
        Ray ray,
        List<DoorDataStruct> doors,
        bool allowClose,
        float SPHERECAST_DISTANCE
    )
    {
        const float SPHERECAST_RADIUS = 0.15f;
        if (Physics.SphereCast(ray, SPHERECAST_RADIUS, out RaycastHit hit, SPHERECAST_DISTANCE, LayersMaskController.PlayerStaticDoorMask))
        {
#if DEBUG
            DebugGizmos.DrawLine(ray.origin, hit.point, Color.red, 0.25f, 30f, true);
#endif
            if (FindDoorFromCollider(hit.collider, out data, out index, doors))
            {
#if DEBUG
                Logger.LogDebug($"Found door from hit collider [PlayerStaticDoorMask] [{hit.collider.name}]");
#endif
                if (allowClose && data.Door.DoorState == EDoorState.Open)
                {
                    interactionType = EInteractionType.Close;
                    return true;
                }
                if (data.Door.DoorState == EDoorState.Shut)
                {
                    interactionType = EInteractionType.Open;
                    return true;
                }
            }
            else
            {
                //Logger.LogDebug($"Failed to find door, but we hit something on [PlayerStaticDoorMask] [{hit.collider.name}]");
            }
        }
        if (Physics.SphereCast(ray, SPHERECAST_RADIUS, out hit, SPHERECAST_DISTANCE, LayersMaskController.DoorLayer))
        {
            DebugGizmos.DrawLine(ray.origin, hit.point, Color.red, 0.25f, 30f, true);
            if (FindDoorFromCollider(hit.collider, out data, out index, doors))
            {
#if DEBUG
                Logger.LogDebug($"Found door from hit collider [DoorLayer] [{hit.collider.name}]");
#endif
                if (allowClose && data.Door.DoorState == EDoorState.Open)
                {
                    interactionType = EInteractionType.Close;
                    return true;
                }
                if (data.Door.DoorState == EDoorState.Shut)
                {
                    interactionType = EInteractionType.Open;
                    return true;
                }
            }
            else
            {
                //Logger.LogDebug($"Failed to find door, but we hit something on [DoorLayer] [{hit.collider.name}]");
            }
        }
        for (index = 0; index < doors.Count; index++)
        {
            data = doors[index];
            if (data.CurrentSqrMagnitude > RAY_LENGTH * RAY_LENGTH)
            {
                continue;
            }

            if (!CanInteract(data.Link))
            {
                continue;
            }

            Collider doorCollider = data.Door.Collider;
            if (doorCollider == null)
            {
                continue;
            }

            if (doorCollider.Raycast(ray, out hit, SPHERECAST_DISTANCE))
            {
#if DEBUG
                Logger.LogDebug($"hit door  [Door.collider.Raycast]");
                DebugGizmos.DrawLine(ray.origin, hit.point, Color.red, 0.25f, 30f, true);
#endif
                if (allowClose && data.Door.DoorState == EDoorState.Open)
                {
                    interactionType = EInteractionType.Close;
                    return true;
                }
                if (data.Door.DoorState == EDoorState.Shut)
                {
                    interactionType = EInteractionType.Open;
                    return true;
                }
            }
        }
        interactionType = EInteractionType.Open; // Default to open if no doors found
        return false;
    }

    private static bool IsDoorOpenable(Door door)
    {
        if (!door.enabled || !door.gameObject.activeInHierarchy || !door.Operatable)
        {
            return false;
        }
        //if (!ModDetection.ProjectFikaLoaded &&
        //    GlobalSettingsClass.Instance.General.Doors.DisableAllDoors &&
        //    GameWorldComponent.Instance.Doors.DisableDoor(door))
        //{
        //    return false;
        //}
        return true;
    }

    private float _nextDoorUpdateTime;

    private static bool CanInteract(NavMeshDoorLink link)
    {
        if (!link.ShallInteract())
        {
            return false;
        }
        if (!link.Door.enabled || !link.Door.gameObject.activeInHierarchy || !link.Door.Operatable)
        {
            return false;
        }
        return true;
    }

    private bool InteractWithDoor(ref DoorDataStruct data, EInteractionType type)
    {
        if (data.Door == null)
        {
            return false;
        }

        switch (data.Door.DoorState)
        {
            case EDoorState.Shut:
                data.LastCloseTime = Time.time;
                break;

            case EDoorState.Open:
                data.LastOpenTime = Time.time;
                break;

            default:
                return false;
        }
        data.LastInteractTime = Time.time;
        Player.MovementContext.ResetCanUsePropState();
        var interactionResult = Door.Interact(Player, type);
        if (interactionResult.Succeeded)
        {
            //Logger.LogDebug("Success");
            switch (type)
            {
                case EInteractionType.Breach:
                    Player.StartInteraction(data.Door, interactionResult.Value, null);
                    break;

                default:
                    Player.ExecuteInteraction(data.Door, interactionResult.Value);
                    break;
            }
            return true;
        }
        return false;
    }

    public bool ShallKickOpen(Door door, EInteractionType Etype)
    {
        if (Etype != EInteractionType.Open)
        {
            return false;
        }
        if (!WantToKick())
        {
            return false;
        }
        var breakInParameters = door.GetBreakInParameters(Bot.Position);
        return door.BreachSuccessRoll(breakInParameters.InteractionPosition);
    }

    private bool WantToKick()
    {
        var enemy = Bot.GoalEnemy;
        if (enemy != null)
        {
            if (Bot.Info.PersonalitySettings.General.KickOpenAllDoors)
            {
                return true;
            }
            if (BotOwner.Memory.IsUnderFire)
            {
                return true;
            }
            float? timeSinceSeen = enemy.TimeSinceSeen;
            if (timeSinceSeen != null)
            {
                if (timeSinceSeen.Value < 3f)
                {
                    return true;
                }
                if (timeSinceSeen.Value < 5f && enemy.InLineOfSight)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsDoorPullOpen(DoorDataStruct doorData, Vector3 botPosition)
    {
        Vector3 doorOpenPos = doorData.Link.Open2;
        Vector3 doorPos = doorData.Link.transform.position;
        Vector3 openDirection = (doorOpenPos - doorPos).normalized;
        Vector3 botDirection = (botPosition - doorPos).normalized;
        float dotProduct = Vector3.Dot(openDirection, botDirection);
        return dotProduct > 0;
    }

    public DoorDataStruct ActiveDoor = new();
    private float _doorInteractionEndTime;
}
