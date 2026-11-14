using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Preset.Shared.GlobalSettings;

namespace SAIN.Layers.Combat.Solo;

/// <summary>
/// Fork: a bot that rolled Fight against a shooter it cannot see faces the vague shot area, fires at it and works toward it (dogfight movement) for a few seconds.
/// The timing and the aim are handled by ShotReactionClass.
/// </summary>
internal class ShotSearchAction(BotOwner bot) : BotAction(bot, "ShotSearch"), IBotAction
{
    public override void Update(CustomLayer.ActionData data)
    {
        if (GlobalSettingsClass.Instance.Fork.ShotFightSearchMove)
        {
            // Dogfight movement toward the vague area (the shot area is the enemy's last known position): strafes and closes in while the bot fires.
            Bot.Mover.SetTargetPose(1f);
            Bot.Mover.DogFight.DogFightMove(true, Bot.GoalEnemy);
            return;
        }
        Bot.Mover.SetTargetMoveSpeed(1f);
        if (Bot.Mover.Moving)
        {
            Bot.Mover.Stop();
        }
    }

    public override void OnSteeringTicked()
    {
        if (Shoot.ShootAnyVisibleEnemies(Bot.GoalEnemy))
        {
            return;
        }
        Bot.ShotReaction.FightSearchTick(Bot.GoalEnemy);
    }

    public override void Start()
    {
        base.Start();
        if (!GlobalSettingsClass.Instance.Fork.ShotFightSearchMove)
        {
            Bot.Mover.Stop();
        }
    }

    public override void Stop()
    {
        base.Stop();
        Bot.Mover.DogFight.ResetDogFightStatus();
        Bot.ShotReaction.FightSearchEnded();
    }
}
