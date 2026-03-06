using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class WoundableBoneComponent : Component
{
    /// <summary>
    /// Bone strength value depending on the current state of the bone.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<BoneStatus, float> StrengthThresholds;

    /// <summary>
    /// Bone health. The lower this value, the less the bone protects internal organs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Health = 100;

    /// <summary>
    /// Maximum bone health.
    /// </summary>
    [DataField]
    public FixedPoint2 MaximumHealth = 100;

    /// <summary>
    /// Bone strength multiplier. The higher this value, the less damage the bone takes and the better it protects internal organs.
    /// </summary>
    [DataField]
    public float StrengthMultiplier = 1f;

    /// <summary>
    /// Damage that can affect bone health.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> SupportedDamageType = new();

    /// <summary>
    /// Bone status which is applied depending on the current health. The lowest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, BoneStatus> BoneStatusThresholds;

    [ViewVariables, AutoNetworkedField]
    public BoneStatus CurrentBoneStatusThreshold = BoneStatus.Whole;

    [ViewVariables]
    public float CurrentStrength => StrengthThresholds[CurrentBoneStatusThreshold] * StrengthMultiplier;
}

[Serializable, NetSerializable]
public enum BoneStatus : byte
{
    Whole,
    Damaged,
    Cracked,
    Broken
}
