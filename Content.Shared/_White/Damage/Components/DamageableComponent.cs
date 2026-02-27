using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using DamageableSystem = Content.Shared._White.Damage.Systems.DamageableSystem;

namespace Content.Shared._White.Damage.Components;

/// <summary>
/// Component that allows entities to take damage.
/// </summary>
/// <remarks>
/// The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. DamageContainers
/// may also have resistances to certain damage types, defined via a <see cref="DamageModifierSetPrototype"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DamageableSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class DamageableComponent : Component
{
    /// <summary>
    /// All the damage information is stored in this <see cref="Shared.Damage.DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    /// If this data-field is specified, this allows damageable components to be initialized with non-zero damage.
    /// </remarks>
    [DataField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// This <see cref="DamageContainerPrototype"/> specifies what damage types are supported by this component.
    /// If null, all damage types will be supported.
    /// </summary>
    [DataField]
    public ProtoId<DamageContainerPrototype>? DamageContainer;

    /// <summary>
    /// This <see cref="DamageModifierSetPrototype"/> will be applied to any damage that is dealt to this container,
    /// unless the damage explicitly ignores resistances.
    /// </summary>
    /// <remarks>
    /// Though DamageModifierSets can be deserialized directly, we only want to use the prototype version here
    /// to reduce duplication.
    /// </remarks>
    [DataField]
    public ProtoId<DamageModifierSetPrototype>? DamageModifierSet;

    /// <summary>
    /// Damage, indexed by <see cref="DamageGroupPrototype"/> ID keys.
    /// </summary>
    /// <remarks>
    /// Groups which have no members that are supported by this component will not be present in this
    /// dictionary.
    /// </remarks>
    [ViewVariables]
    public Dictionary<string, FixedPoint2> DamagePerGroup = new();

    /// <summary>
    /// The sum of all damages in the DamageableComponent.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 TotalDamage;

    [DataField]
    public List<ProtoId<DamageTypePrototype>> RadiationDamageTypes = new() { "Radiation" };
}

[Serializable, NetSerializable]
public sealed class DamageableComponentState(DamageableComponent component) : ComponentState
{
    public readonly DamageSpecifier Damage = new (component.Damage);
    public readonly ProtoId<DamageContainerPrototype>? DamageContainer = component.DamageContainer;
    public readonly ProtoId<DamageModifierSetPrototype>? DamageModifierSet = component.DamageModifierSet;
}
