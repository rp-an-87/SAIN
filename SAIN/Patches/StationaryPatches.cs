using System.Reflection;
using HarmonyLib;
using SAIN.Components;
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
}
