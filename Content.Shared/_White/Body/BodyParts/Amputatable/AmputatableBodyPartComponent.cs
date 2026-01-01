using Content.Shared._White.Medical.Wounds.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.BodyParts.Amputatable;

[RegisterComponent]
public sealed partial class AmputatableBodyPartComponent : Component
{
    /// <summary>
    /// The wound that will be created after amputation of this limb.
    /// </summary>
    [DataField]
    public EntProtoId<WoundComponent> Wound;

    /// <summary>
    /// Damage, the value of which affects the chance of amputating a body part.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, float> SupportedDamageType = new();

    /// <summary>
    /// Chance of amputated body parts based on damage. The highest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, float> ChanceThresholds;

    /// <summary>
    /// A multiplier for the chance of body part amputation based on the current bone status.
    /// </summary>
    [DataField]
    public Dictionary<BoneStatus, float> BoneMultiplierThresholds= new();

    [ViewVariables]
    public FixedPoint2 TotalDamage = FixedPoint2.Zero;

    [ViewVariables]
    public float CurrentChance => float.Clamp(CurrentChanceThreshold * CurrentBoneMultiplierThreshold, 0f, 1f);

    [ViewVariables]
    public float CurrentChanceThreshold;

    [ViewVariables]
    public float CurrentBoneMultiplierThreshold = 1f;
}
