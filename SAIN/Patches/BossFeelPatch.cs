using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SAIN.Patches.Generic;

/// <summary>
/// Fork: Tagilla's vanilla "feel" branch (TagillaEnemyChooser.FindDangerEnemy, while he waits in his Ambush layer) picks the nearest non-scav enemy but only
/// accepts real players (!Person.IsAI), so he never takes an AI PMC that is shooting him. With BossFeelIncludesAI that test is replaced: AI PMCs count too.
/// Scavs stay excluded by the vanilla side check (Side != Savage). The check is made at run time, so the setting can be switched without a restart of the patch.
/// </summary>
public class TagillaFeelIncludesAI_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TagillaEnemyChooser), nameof(TagillaEnemyChooser.FindDangerEnemy));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo isAi = AccessTools.PropertyGetter(typeof(IPlayer), nameof(IPlayer.IsAI));
        MethodInfo replacement = AccessTools.Method(typeof(TagillaFeelIncludesAI_Patch), nameof(IsAIForFeel));
        int replaced = 0;
        var result = new List<CodeInstruction>();
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(isAi))
            {
                var changed = new CodeInstruction(OpCodes.Call, replacement);
                changed.labels.AddRange(instruction.labels);
                changed.blocks.AddRange(instruction.blocks);
                result.Add(changed);
                replaced++;
                continue;
            }
            result.Add(instruction);
        }
        if (replaced == 0)
        {
            global::SAIN.Logger.LogWarning("[BossFeel] TagillaEnemyChooser.FindDangerEnemy: IsAI test not found, patch has no effect");
        }
        else
        {
            global::SAIN.Logger.LogInfoAlways($"[BossFeel] TagillaEnemyChooser.FindDangerEnemy: {replaced} IsAI test(s) replaced");
        }
        return result;
    }

    /// <summary>Stands in for IPlayer.IsAI: false for every player when the setting is on, so AI PMCs pass the "feel" filter.</summary>
    public static bool IsAIForFeel(IPlayer player)
    {
        var settings = SAINPlugin.LoadedPreset?.GlobalSettings?.Fork;
        if (settings != null && settings.BossFeelIncludesAI)
        {
            return false;
        }
        return player.IsAI;
    }
}
