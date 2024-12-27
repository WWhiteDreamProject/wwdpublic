using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class InsideAlienLarvaComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphPrototype = "AlienEvolutionGrowStageTwo";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EvolutionAction = "ActionLarvaGrow";

    [DataField]
    public EntityUid? EvolutionActionEntity;

    [DataField]
    public TimeSpan EvolutionCooldown = TimeSpan.Zero;

    public bool IsGrown;
}

public sealed partial class AlienLarvaGrowActionEvent : InstantActionEvent { }
