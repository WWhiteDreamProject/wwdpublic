using Content.Shared._White.Body.Wounds.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Providers.Amputatable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(AmputatableBodyProviderSystem))]
public sealed partial class AmputatableBodyProviderComponent : Component
{
    /// <summary>
    /// A multiplier for the chance of body provider amputation based on the current bone status.
    /// </summary>
    [DataField]
    public Dictionary<BoneStatus, float> BoneMultiplierThresholds = new();

    /// <summary>
    /// Damage, the value of which affects the chance of amputating a body provider.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, float> SupportedDamageType = new();

    /// <summary>
    /// The wound that will be created after amputation of this limb.
    /// </summary>
    [DataField]
    public EntProtoId<WoundComponent> Wound;

    /// <summary>
    /// Chance of amputated body provider based on damage. The highest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, float> ChanceThresholds;

    /// <summary>
    /// The parent entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Parent;

    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 TotalDamage = FixedPoint2.Zero;

    [ViewVariables, AutoNetworkedField]
    public float CurrentChanceThreshold = 1f;

    [ViewVariables, AutoNetworkedField]
    public float CurrentBoneMultiplierThreshold = 1f;

    [ViewVariables]
    public float CurrentChance => float.Clamp(CurrentChanceThreshold * CurrentBoneMultiplierThreshold, 0f, 1f);
}
