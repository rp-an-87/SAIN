using EFT;
using SAIN.Helpers;
using SAIN.Preset.Shared.GlobalSettings.Categories;

namespace SAIN.SAINComponent.Classes.Info;

/// <summary>
/// Bot groups for the fork's per-brain toggles (Fork Tweaks: Range and Shot Reaction each have their own "Brain filter").
/// </summary>
public enum EForkBrainGroup
{
    Other,
    Pmc,
    Raider,
    Rogue,
    Scav,
    ScavSniper,
    Cultist,
    Boss,
    Follower,
    Goon,
    Event,
}

public static class ForkBrainFilter
{
    public static EForkBrainGroup GetGroup(WildSpawnType type)
    {
        switch (type)
        {
            case WildSpawnType.pmcUSEC:
            case WildSpawnType.pmcBEAR:
                return EForkBrainGroup.Pmc;
            case WildSpawnType.pmcBot:
                return EForkBrainGroup.Raider;
            case WildSpawnType.exUsec:
                return EForkBrainGroup.Rogue;
            case WildSpawnType.marksman:
                return EForkBrainGroup.ScavSniper;
            case WildSpawnType.assault:
            case WildSpawnType.assaultGroup:
            case WildSpawnType.cursedAssault:
                return EForkBrainGroup.Scav;
        }

        if (EnumValues.WildSpawn.IsSpecialBot(type))
        {
            return EForkBrainGroup.Event;
        }
        if (EnumValues.WildSpawn.IsGoons(type))
        {
            return EForkBrainGroup.Goon;
        }
        if (type.IsSectant())
        {
            return EForkBrainGroup.Cultist;
        }
        if (type.IsFollower())
        {
            return EForkBrainGroup.Follower;
        }
        if (type.IsBoss())
        {
            return EForkBrainGroup.Boss;
        }
        return EForkBrainGroup.Other;
    }

    public static bool IsRangeEnabled(EForkBrainGroup group, ForkSettings fork)
    {
        switch (group)
        {
            case EForkBrainGroup.Pmc:
                return fork.RangeBrainPmc;
            case EForkBrainGroup.Rogue:
                return fork.RangeBrainRogue;
            case EForkBrainGroup.Raider:
                return fork.RangeBrainRaider;
            case EForkBrainGroup.Scav:
                return fork.RangeBrainScav;
            case EForkBrainGroup.ScavSniper:
                return fork.RangeBrainScavSniper;
            case EForkBrainGroup.Cultist:
                return fork.RangeBrainCultist;
            case EForkBrainGroup.Boss:
                return fork.RangeBrainBoss;
            case EForkBrainGroup.Follower:
                return fork.RangeBrainFollower;
            case EForkBrainGroup.Goon:
                return fork.RangeBrainGoon;
            case EForkBrainGroup.Event:
                return fork.RangeBrainEvent;
            default:
                return fork.RangeBrainOther;
        }
    }

    public static bool IsShotReactionEnabled(EForkBrainGroup group, ForkSettings fork)
    {
        switch (group)
        {
            case EForkBrainGroup.Pmc:
                return fork.ShotBrainPmc;
            case EForkBrainGroup.Rogue:
                return fork.ShotBrainRogue;
            case EForkBrainGroup.Raider:
                return fork.ShotBrainRaider;
            case EForkBrainGroup.Scav:
                return fork.ShotBrainScav;
            case EForkBrainGroup.ScavSniper:
                return fork.ShotBrainScavSniper;
            case EForkBrainGroup.Cultist:
                return fork.ShotBrainCultist;
            case EForkBrainGroup.Boss:
                return fork.ShotBrainBoss;
            case EForkBrainGroup.Follower:
                return fork.ShotBrainFollower;
            case EForkBrainGroup.Goon:
                return fork.ShotBrainGoon;
            case EForkBrainGroup.Event:
                return fork.ShotBrainEvent;
            default:
                return fork.ShotBrainOther;
        }
    }
}
