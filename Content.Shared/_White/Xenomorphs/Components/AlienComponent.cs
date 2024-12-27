using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class AlienComponent : Component
{
    // Actions

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DevourAction = "ActionDevour";

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current plasma of the mob making node.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float PlasmaCostNode = 50f;

    /// <summary>
    /// The node prototype to use.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WeednodePrototype = "ResinWeedNode";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? WeednodeAction = "ActionResinNode";

    [DataField]
    public string Caste;

    [DataField]
    public EntityUid? WeednodeActionEntity;

    [DataField(required: true)]
    public DamageSpecifier WeedHeal;

}

public sealed partial class WeednodeActionEvent : InstantActionEvent { }
