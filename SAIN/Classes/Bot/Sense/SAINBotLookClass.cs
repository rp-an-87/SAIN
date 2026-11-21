using EFT;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes;

public class SAINBotLookClass : BotBase
{
    public SAINBotLookClass(BotComponent component)
        : base(component)
    {
        LookData = new LookAllData();
    }

    public readonly LookAllData LookData;

    // This (And the methods it calls) mirrors a large part of BSG's LookSensor
    // Look at that for potential changes between versions
    public int UpdateLook(float currentTime)
    {
        if (BotOwner.LeaveData == null || BotOwner.LeaveData.LeaveComplete)
        {
            return 0;
        }

        int numUpdated = UpdateLookForEnemies(LookData, currentTime, Bot);
        UpdateLookData(LookData);
        return numUpdated;
    }

    // Fork: the game's per-tick "visible" flag can stay true without any sight (seen in a raid: one bot reported the player about 18 times a second for 15 minutes
    // while the player was out of sight, and the group followed the exact position the whole time). A report only goes out while SAIN's own sight check
    // (real raycasts to the player's colliders) has passed within this many seconds; otherwise it is dropped and the game's flag is reset.
    private const float REPORT_SIGHT_GRACE = 1.0f;
    private const float REPORT_DROP_WARN_AFTER = 3.0f;
    private const float REPORT_DROP_WARN_EVERY = 30.0f;

    private float _dropStartTime;
    private float _nextDropWarn;

    public void UpdateLookData(LookAllData lookData)
    {
        int reported = 0;
        for (int i = 0; i < lookData.ReportsData.Count; i++)
        {
            ReportAiData enemyVision = lookData.ReportsData[i];
            if (!HasRealSight(enemyVision.Enemy))
            {
                continue;
            }
            reported++;
            BotOwner.BotsGroup.ReportAboutEnemy(enemyVision.Enemy, enemyVision.VisibleOnlyBySence, BotOwner);
        }

        if (reported > 0)
        {
            BotOwner.Memory.SetLastTimeSeeEnemy();
        }

        if (lookData.ShallRecalcGoal)
        {
            //BotOwner.CalcGoal();
        }

        lookData.Reset();
    }

    /// <summary>True when SAIN's own sight check has passed for this enemy recently. An enemy SAIN does not track yet is let through (nothing to check against).</summary>
    private bool HasRealSight(IPlayer player)
    {
        Enemy enemy = player == null ? null : Bot.EnemyController?.GetEnemy(player.ProfileId, false);
        if (enemy?.Vision?.EnemyParts == null)
        {
            return true;
        }
        float now = Time.time;
        if (now - enemy.Vision.EnemyParts.TimeLastCanBeSeen <= REPORT_SIGHT_GRACE)
        {
            _dropStartTime = 0f;
            return true;
        }
        try
        {
            if (enemy.EnemyInfo != null && enemy.EnemyInfo.IsVisible)
            {
                enemy.EnemyInfo.SetVisible(false);
            }
        }
        catch
        { /* eft code */
        }
        if (_dropStartTime <= 0f)
        {
            _dropStartTime = now;
        }
        else if (now - _dropStartTime > REPORT_DROP_WARN_AFTER && now >= _nextDropWarn && enemy.EnemyPlayer?.IsYourPlayer == true)
        {
            _nextDropWarn = now + REPORT_DROP_WARN_EVERY;
            Logger.LogInfoAlways($"[ReportGate] t={now:0.0} {Bot.name}: the game says it sees you ({enemy.RealDistance:0} m) but SAIN's sight check has failed for {now - _dropStartTime:0} s; reports dropped");
        }
        return false;
    }

    private static int UpdateLookForEnemies(LookAllData lookAll, float currentTime, BotComponent bot)
    {
        int updated = 0;
        var lookSensor = bot.BotOwner.LookSensor;

        var transform = bot.Transform;
        Vector3 viewPosition = transform.EyePosition;
        var weaponRoot = transform.WeaponRoot;

        // Update look sensors fields since we are not calling the original botowner code that does this.
        // We should check for changes between tarkov updates.
        lookSensor._weaponRootPoint = weaponRoot;
        lookSensor._lookSensorShootPosition.UpdateShootPosition(weaponRoot);
        lookSensor.HeadPoint = viewPosition;

        lookAll.Reset();
        var enemies = bot.EnemyController.EnemiesArray;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.ShallCheckLook(currentTime, out float deltaTime))
            {
                enemy.EnemyInfo.CheckLookEnemy(lookAll, deltaTime);
                updated++;
            }
        }

        return updated;
    }
}
