using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Models.Enums;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo;

/// <summary>
/// Fork: after hiding from an unseen shooter, the bot runs to a different area instead of going back to where it was shot at.
/// The destination is chosen by ShotReactionClass.
/// </summary>
internal class RelocateAction(BotOwner bot) : BotAction(bot, "Relocate"), IBotAction
{
    private const float REPATH_INTERVAL = 1.5f;

    public override void Update(CustomLayer.ActionData data)
    {
        Bot.Mover.SetTargetMoveSpeed(1f);
        Bot.Mover.SetTargetPose(1f);

        // A leg of the route is done: the next one starts at once (no pause at the waypoint).
        if (Bot.ShotReaction.UpdateRouteProgress())
        {
            _nextRepathTime = 0f;
        }
        if (_nextRepathTime > Time.time)
        {
            return;
        }
        _nextRepathTime = Time.time + REPATH_INTERVAL;
        Bot.Mover.RunToPoint(Bot.ShotReaction.CurrentWaypoint, true, 2f, ESprintUrgency.High);
    }

    public override void OnSteeringTicked()
    {
        if (Shoot.ShootAnyVisibleEnemies(Bot.GoalEnemy))
        {
            return;
        }
        Bot.Steering.LookToMovingDirection();
    }

    public override void Start()
    {
        base.Start();
        _nextRepathTime = 0f;
    }

    public override void Stop()
    {
        base.Stop();
        Bot.Mover.SetTargetMoveSpeed(1f);
        Bot.Mover.SetTargetPose(1f);
    }

    private float _nextRepathTime;
}
