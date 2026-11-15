using System.Reflection;
using HarmonyLib;
using EFT;
using EFT.Ballistics;
using SAIN.Components;
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
            if (!fork.RogueLimitedBroadcast || damageinfo.Player == null || victim == null || !victim.IsAI || victim.Profile?.Info?.Settings?.Role != WildSpawnType.exUsec)
            {
                return true;
            }
            BotOwner owner = __instance._owner;
            if (owner == null || victim == owner.GetPlayer)
            {
                return true;
            }
            float distance = (victim.Position - owner.Position).magnitude;
            bool allowed = distance <= fork.ShotRogueAlertRange && Random.value < fork.ShotRogueAlertChance;
            if (fork.ShotReactionLog)
            {
                global::SAIN.Logger.LogInfoAlways($"[RogueRadio] t={Time.time:0.0} {owner.name}: hit Rogue {victim.Profile.Nickname} was {distance:0} m away (range {fork.ShotRogueAlertRange:0}, chance {fork.ShotRogueAlertChance:0.00}): {(allowed ? "takes the shooter as an enemy" : "hears nothing")}");
            }
            return allowed;
        }
    }
}
