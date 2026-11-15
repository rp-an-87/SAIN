using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using EFT;
using EFT.Ballistics;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using SPT.Reflection.Patching;

namespace SAIN.Patches.Generic
{
    /// <summary>
    /// Fork: the vanilla Rogue gun layer (StationaryWS, priority 75) outranks every SAIN layer. While a bot has left its mounted gun because of a shot
    /// (ShotReactionClass.StationaryBlocked) the layer is not allowed to take it back, so the shot reaction can actually run.
    /// </summary>
    public class StationaryLayerGate_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StationaryWithSuppressLayer), nameof(StationaryWithSuppressLayer.ShallUseNow));
        }

        [PatchPostfix]
        public static void Patch(StationaryWithSuppressLayer __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            if (BotManagerComponent.Instance?.GetSAIN(__instance._owner, out BotComponent bot) == true && bot.ShotReaction?.StationaryBlocked == true)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Fork: vanilla tells every Rogue of an attack zone the exact position of a player who enters the zone and marks the player visible to them (a map-wide cheat).
    /// With RogueNoZoneMagic the whole call is skipped: the Rogues have to find the player by sight and sound.
    /// </summary>
    public class RogueZoneMagic_Patch : ModulePatch
    {
        private static float _nextLog;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecLayersStrategy), nameof(ExUsecLayersStrategy.OnPlayerEnter));
        }

        [PatchPrefix]
        public static bool Patch(ExUsecLayersStrategy __instance, Player obj)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Fork.RogueNoZoneMagic)
            {
                return true;
            }
            if (SAINPlugin.LoadedPreset.GlobalSettings.Fork.ShotReactionLog && Time.time >= _nextLog)
            {
                _nextLog = Time.time + 5f;
                global::SAIN.Logger.LogInfoAlways($"[RogueRadio] t={Time.time:0.0} {__instance._owner?.name}: zone entry by {obj?.Profile?.Nickname} ignored (no exact position, no forced visibility)");
            }
            return false;
        }
    }

    /// <summary>
    /// Fork: vanilla makes every Rogue on the map take the shooter of a hit Rogue as an enemy. With RogueLimitedBroadcast only Rogues within the Rogue Alert Range
    /// of the victim take it, each with the Rogue Alert Chance (the victim always does).
    /// </summary>
    public class RogueHitBroadcast_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecLayersStrategy), nameof(ExUsecLayersStrategy.OnSomeBodyBeingHit));
        }

        [PatchPrefix]
        public static bool Patch(ExUsecLayersStrategy __instance, DamageInfo damageinfo, Player victim)
        {
            var fork = SAINPlugin.LoadedPreset.GlobalSettings.Fork;
            if (!fork.RogueLimitedBroadcast || damageinfo.Player == null || damageinfo.Player.iPlayer?.AIData?.IsAI == true || victim == null || !victim.IsAI || victim.Profile?.Info?.Settings?.Role != WildSpawnType.exUsec)
            {
                return true;
            }
            BotOwner owner = __instance._owner;
            if (owner == null || victim == owner.GetPlayer)
            {
                return true;
            }
            float distance = (victim.Position - owner.Position).magnitude;
            bool allowed = (fork.ShotRogueAlertRange <= 0f || distance <= fork.ShotRogueAlertRange) && Random.value < fork.ShotRogueAlertChance;
            if (fork.ShotReactionLog)
            {
                global::SAIN.Logger.LogInfoAlways($"[RogueRadio] t={Time.time:0.0} {owner.name}: hit Rogue {victim.Profile.Nickname} was {distance:0} m away (range {fork.ShotRogueAlertRange:0}, chance {fork.ShotRogueAlertChance:0.00}): {(allowed ? "takes the shooter as an enemy" : "hears nothing")}");
            }
            return allowed;
        }
    }

    internal static class MountedHelpers
    {
        private static readonly Dictionary<int, (float time, Vector2 offset)> _errors = [];
        private static readonly Dictionary<int, (float time, Vector2 offset)> _scatter = [];
        private static float _nextLog;

        public static BotComponent GetBot(BotOwner owner)
        {
            if (owner != null && BotManagerComponent.Instance?.GetSAIN(owner, out BotComponent bot) == true)
            {
                return bot;
            }
            return null;
        }

        public static void Log(string message)
        {
            var fork = SAINPlugin.LoadedPreset.GlobalSettings.Fork;
            if (fork.ShotReactionLog && Time.time >= _nextLog)
            {
                _nextLog = Time.time + 5f;
                global::SAIN.Logger.LogInfoAlways($"[RogueRadio] t={Time.time:0.0} {message}");
            }
        }

        /// <summary>The last position the bot really knew for this enemy plus a stable error (re-rolled every 3 s). Null if it never knew one.</summary>
        public static Vector3? KnownPosition(BotComponent bot, string profileId)
        {
            var fork = SAINPlugin.LoadedPreset.GlobalSettings.Fork;
            Enemy enemy = bot.EnemyController?.GetEnemy(profileId, false);
            Vector3? known = enemy?.LastKnownPosition;
            if (known == null)
            {
                return null;
            }
            int id = bot.BotOwner.Id;
            if (!_errors.TryGetValue(id, out var entry) || Time.time - entry.time > 3f)
            {
                entry = (Time.time, Random.insideUnitCircle * fork.RogueMountedAimError);
                _errors[id] = entry;
            }
            return known.Value + new Vector3(entry.offset.x, 0f, entry.offset.y);
        }

        /// <summary>For the suppression target getters: swaps the live position for the known one (leaves it alone for bots or enemies it cannot judge).</summary>
        public static void ApplyKnown(EnemyInfo info, ref Vector3 result)
        {
            if (info == null || info.Person?.AIData?.IsAI == true || !SAINPlugin.LoadedPreset.GlobalSettings.Fork.RogueMountedKnownPosition)
            {
                return;
            }
            BotComponent bot = GetBot(info.Owner);
            if (bot == null)
            {
                return;
            }
            Vector3? known = KnownPosition(bot, info.ProfileId);
            if (known != null)
            {
                result = known.Value;
            }
        }

        /// <summary>Do not start mounted suppression unless the bot really knows its goal enemy.</summary>
        public static bool AllowSuppress(ExUsecLayersStrategy strategy)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Fork.RogueMountedKnownPosition)
            {
                return true;
            }
            BotComponent bot = GetBot(strategy._owner);
            if (bot == null)
            {
                return true;
            }
            Enemy goal = bot.GoalEnemy;
            if (goal != null && goal.IsAI)
            {
                return true;
            }
            if (goal == null || !goal.EnemyKnown)
            {
                Log($"{bot.name}: mounted suppression not started (the player is not known)");
                return false;
            }
            float cap = SAINPlugin.LoadedPreset.GlobalSettings.Fork.MountedGrenadeMaxRange;
            Vector3? known = goal.LastKnownPosition;
            if (strategy._isGrenade && cap > 0f && known != null && !goal.IsVisible && (known.Value - bot.Position).magnitude > cap)
            {
                Log($"{bot.name}: grenade launcher suppression not started (the player is beyond its range cap of {cap:0} m)");
                return false;
            }
            return true;
        }

        public static Vector3 GrenadeScatter(BotOwner owner, Vector3 aimPoint)
        {
            var fork = SAINPlugin.LoadedPreset.GlobalSettings.Fork;
            if (!fork.GrenadeLauncherScatter || owner == null)
            {
                return Vector3.zero;
            }
            var stationary = owner.WeaponManager?.Stationary;
            if (stationary == null || !stationary.Taken || stationary.CurLink == null || !stationary.CurLink.IsGrenade())
            {
                return Vector3.zero;
            }
            if (GetBot(owner)?.GoalEnemy?.IsAI == true)
            {
                return Vector3.zero;
            }
            float distance = (aimPoint - owner.Position).magnitude;
            float radius = Mathf.Clamp((distance - fork.GrenadeScatterStart) * fork.GrenadeScatterPerMeter, 0f, fork.GrenadeScatterMax);
            if (radius <= 0.01f)
            {
                return Vector3.zero;
            }
            int id = owner.Id;
            if (!_scatter.TryGetValue(id, out var entry) || Time.time - entry.time > 0.5f)
            {
                entry = (Time.time, Random.insideUnitCircle * radius);
                _scatter[id] = entry;
                Enemy goal = GetBot(owner)?.GoalEnemy;
                Log($"{owner.name}: grenade launcher aims at {distance:0} m, scatter radius {radius:0} m, target visible {goal?.IsVisible == true}");
            }
            return new Vector3(entry.offset.x, 0f, entry.offset.y);
        }
    }

    /// <summary>Fork: vanilla enemy reports between Rogues give every Rogue the exact position at once. Delayed and range-limited here.</summary>
    public class RogueReport_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecAbstractStrategy), nameof(ExUsecAbstractStrategy.OnReportEnemy));
        }

        [PatchPrefix]
        public static bool Patch(ExUsecAbstractStrategy __instance, IPlayer enemy, Vector3 enemyPos, Vector3 weaponRootLast, EEnemyPartVisibleType isVisibleOnlyBySense, BotOwner reporter)
        {
            var fork = SAINPlugin.LoadedPreset.GlobalSettings.Fork;
            if (!fork.RogueReportDelayed || enemy?.AIData?.IsAI == true)
            {
                return true;
            }
            BotOwner owner = __instance._owner;
            BotComponent bot = MountedHelpers.GetBot(owner);
            if (bot == null)
            {
                return true;
            }
            float distance = reporter != null ? (reporter.Position - owner.Position).magnitude : 0f;
            if (fork.RogueReportRange > 0f && distance > fork.RogueReportRange)
            {
                return false;
            }
            float delay = Random.Range(fork.RogueReportDelayMin, Mathf.Max(fork.RogueReportDelayMin, fork.RogueReportDelayMax)) + distance * fork.RogueReportDelayPerMeter;
            bot.ShotReaction.QueueReport(enemy, enemyPos, weaponRootLast, Time.time + delay);
            MountedHelpers.Log($"{owner.name}: report of {enemy?.Profile?.Nickname} from {reporter?.name} ({distance:0} m) arrives in {delay:0.0} s");
            return false;
        }
    }

    /// <summary>Fork: a mounted gun aims at the last position the Rogue knew, not at the player's live position.</summary>
    public class MountedAim_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AimingFromStationary), nameof(AimingFromStationary.GetTarget));
        }

        [PatchPrefix]
        public static bool Patch(AimingFromStationary __instance, ref Vector3? __result)
        {
            var fork = SAINPlugin.LoadedPreset.GlobalSettings.Fork;
            if (!fork.RogueMountedKnownPosition)
            {
                return true;
            }
            BotOwner owner = __instance._owner;
            BotComponent bot = MountedHelpers.GetBot(owner);
            if (bot == null)
            {
                return true;
            }
            EnemyInfo goal = owner.Memory.GoalEnemy;
            if (goal == null)
            {
                __result = null;
                return false;
            }
            if ((goal.CanShoot && goal.IsVisible) || goal.Person?.AIData?.IsAI == true)
            {
                return true;
            }
            Vector3? known = MountedHelpers.KnownPosition(bot, goal.ProfileId);
            __result = known != null ? known.Value + Vector3.up * owner.Settings.FileSettings.Aiming.DANGER_UP_POINT : (Vector3?)null;
            return false;
        }
    }

    public class MountedSuppressMg_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecLayersStrategy.CG_FindToSuppressForMG), "method_0");
        }

        [PatchPostfix]
        public static void Patch(ExUsecLayersStrategy.CG_FindToSuppressForMG __instance, ref Vector3 __result)
        {
            MountedHelpers.ApplyKnown(__instance.enemy, ref __result);
        }
    }

    public class MountedSuppressGrenade_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecLayersStrategy.CG_FindToSuppressForGrenade), "method_0");
        }

        [PatchPostfix]
        public static void Patch(ExUsecLayersStrategy.CG_FindToSuppressForGrenade __instance, ref Vector3 __result)
        {
            MountedHelpers.ApplyKnown(__instance.enemyInfo.Value, ref __result);
        }
    }

    public class MountedFindMg_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecLayersStrategy), nameof(ExUsecLayersStrategy.FindToSuppressForMG));
        }

        [PatchPrefix]
        public static bool Patch(ExUsecLayersStrategy __instance)
        {
            return MountedHelpers.AllowSuppress(__instance);
        }
    }

    public class MountedFindGrenade_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecLayersStrategy), nameof(ExUsecLayersStrategy.FindToSuppressForGrenade));
        }

        [PatchPrefix]
        public static bool Patch(ExUsecLayersStrategy __instance)
        {
            return MountedHelpers.AllowSuppress(__instance);
        }
    }
}
