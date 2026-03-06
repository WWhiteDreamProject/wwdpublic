using Content.Shared._White.Body.Wounds.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Body.BodyParts.Gibbable;

[RegisterComponent]
public sealed partial class GibbableBodyPartComponent : Component
{
    /// <summary>
    /// The wound that will be created after gib of this limb.
    /// </summary>
    [DataField]
    public EntProtoId<WoundComponent> Wound;

    /// <summary>
    /// Damage, the value of which affects the chance of gibbing a body part.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, float> SupportedDamageType = new();

    /// <summary>
    /// Chance of gibbed body parts based on damage. The highest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, float> ChanceThresholds;

    /// <summary>
    /// A multiplier for the chance of body part gib based on the current bone status.
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
