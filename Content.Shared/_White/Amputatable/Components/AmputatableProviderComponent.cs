using Content.Shared._White.Amputatable.Systems;
using Content.Shared._White.Wounds;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Amputatable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(AmputatableSystem))]
public sealed partial class AmputatableProviderComponent : Component
{
    /// <summary>
    /// A dictionary that maps specific damage types to an efficiency factor.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, float> SupportedDamage = new();

    /// <summary>
    /// A dictionary that modifies the amputating chance based on the severity of the skeleton's wounds.
    /// </summary>
    [DataField]
    public Dictionary<WoundSeverity, float> SkeletonThresholds = new();

    /// <summary>
    /// The prototype ID of a wound that will be created on the <see cref="Parent"/> if this provider amputee.
    /// If null, no specific wound is created upon amputating.
    /// </summary>
    [DataField]
    public EntProtoId? Wound;

    /// <summary>
    /// The parent entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Parent;

    /// <summary>
    /// A set of damage thresholds and their corresponding amputating chances.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, float> Thresholds;

    /// <summary>
    /// The accumulated damage currently applied to this provider.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Damage = FixedPoint2.Zero;

    /// <summary>
    /// The base chance of amputating this provider, determined by <see cref="Thresholds"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Chance = 0f;

    /// <summary>
    /// The calculated, effective chance of amputating.
    /// </summary>
    [ViewVariables]
    public float CurrentChance => float.Clamp(Chance * SkeletonMultiplier, 0f, 1f);

    /// <summary>
    /// The current multiplier affecting the amputating chance. This is typically updated based on skeleton severity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float SkeletonMultiplier = 1f;
}
