using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class WoundableOrganComponent : Component
{
    /// <summary>
    /// Organ health.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Health = 100;

    /// <summary>
    /// Maximum organ health.
    /// </summary>
    [DataField]
    public FixedPoint2 MaximumHealth = 100;

    /// <summary>
    /// Damage that can affect organ health.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> SupportedDamageType = new();

    /// <summary>
    /// Organ status which is applied depending on the current health. The lowest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, OrganStatus> OrganStatusThresholds;

    [ViewVariables, AutoNetworkedField]
    public OrganStatus CurrentOrganStatusThreshold = OrganStatus.Healthy;
}

[Serializable, NetSerializable]
public enum OrganStatus : byte
{
    Healthy,
    Bruised,
    Damaged,
    Critical,
    Dead
}
