using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Preset.Shared.GlobalSettings.Categories;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo;

/// <summary>
/// Fork: after hiding from an unseen shooter, the bot walks cautiously to a different area instead of going back to where it was shot at.
/// The destination is chosen by ShotReactionClass.
/// </summary>
internal class RelocateAction(BotOwner bot) : BotAction(bot, "Relocate"), IBotAction
{
    private const float REPATH_INTERVAL = 1.5f;

    public override void Update(CustomLayer.ActionData data)
    {
        ForkSettings fork = SAINPlugin.LoadedPreset.GlobalSettings.Fork;
        Bot.Mover.SetTargetMoveSpeed(fork.ShotRelocateSpeed);
        Bot.Mover.SetTargetPose(fork.ShotRelocatePose);

        if (_nextRepathTime > Time.time)
        {
            return;
        }
        _nextRepathTime = Time.time + REPATH_INTERVAL;
        Bot.Mover.WalkToPoint(Bot.ShotReaction.RelocatePoint, true, 2f);
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
