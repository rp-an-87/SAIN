using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories;

[DataContract]
public class AimTargetSettings : SAINSettingsBase<AimTargetSettings>, ISAINSettings
{
    [DataMember]
    [Name("Dynamic Aim Target Selection")]
    [Description(
        "Bots pick which body part to shoot by weight from the parts they can actually hit. "
            + "For the old always-center-mass behaviour, set the head, arm and leg weights to 0. "
            + "Disable to fall back to vanilla behaviour"
    )]
    public bool Enabled = true;

    [DataMember]
    [Name("Part Weights")]
    public AimTargetWeights Weights = new();

    [DataMember]
    [Name("Aim Point Spread")]
    [Description(
        "How much of the chosen body part a bot will spread its aim across, where 1 uses the whole part and 0 always aims at the same point on it."
    )]
    [Percentage0to1]
    public float AimPointSpread = 1f;

    [DataMember]
    [Name("Re-pick Time - Min")]
    [Description("Minimum seconds a bot sticks with a chosen body part before rolling again.")]
    [MinMax(0.1f, 10f, 100f)]
    public float RepickTimeMin = 0.8f;

    [DataMember]
    [Name("Re-pick Time - Max")]
    [MinMax(0.1f, 10f, 100f)]
    public float RepickTimeMax = 2.5f;

    [DataMember]
    [Name("Limb Falloff - Start Distance")]
    [Description("Distance at which bots start favouring the torso over the head and limbs.")]
    [MinMax(10f, 200f, 10f)]
    public float LimbFalloffStart = 45f;

    [DataMember]
    [Name("Limb Falloff - End Distance")]
    [Description("Past this distance limb weights reach zero and bots only shoot at the torso.")]
    [MinMax(20f, 300f, 10f)]
    public float LimbFalloffEnd = 120f;

    [DataMember]
    [Name("Close Range Torso Distance")]
    [Description("Below this distance bots always aim at the torso.")]
    [MinMax(0f, 15f, 100f)]
    public float CloseRangeTorsoDistance = 2f;

    [DataMember]
    [Name("Head Only Scatter Radius")]
    [Description(
        "When the head is the only part of an enemy a bot can shoot (peeking over cover), the normal head rules decide (profile Aim For Head, its chance, the Head weight). "
            + "If they do not pick the head, the bot still aims at the visible head but with this much extra aim error: a random offset (meters, at the target) in a disc around the aim point, re-rolled every 0.3 s. "
            + "Applies to every weapon, including semi-auto and scoped ones. 0 = off (vanilla fallback, shoots the visible head accurately)."
    )]
    [MinMax(0f, 3f, 100f)]
    public float HeadOnlyScatterRadius = 0.6f;
}
