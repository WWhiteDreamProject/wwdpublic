using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    /// All the damage information is stored in this <see cref="DamageSpecifier"/>.
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
    public ProtoId<DamageContainerPrototype>? Container;

    /// <summary>
    /// This <see cref="DamageModifierSetPrototype"/> will be applied to any damage dealt to this container,
    /// unless the damage explicitly ignores resistances.
    /// </summary>
    /// <remarks>
    /// Though DamageModifierSets can be deserialized directly, we only want to use the prototype version here
    /// to reduce duplication.
    /// </remarks>
    [DataField]
    public ProtoId<DamageModifierSetPrototype>? ModifierSet;

    /// <summary>
    /// Damage, indexed by <see cref="DamageGroupPrototype"/> ID keys.
    /// </summary>
    /// <remarks>
    /// Groups which have no members that are supported by this component will not be present in this
    /// dictionary.
    /// </remarks>
    [ViewVariables]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> DamagePerGroup = new();

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
    public readonly ProtoId<DamageContainerPrototype>? Container = component.Container;
    public readonly ProtoId<DamageModifierSetPrototype>? ModifierSet = component.ModifierSet;
}
