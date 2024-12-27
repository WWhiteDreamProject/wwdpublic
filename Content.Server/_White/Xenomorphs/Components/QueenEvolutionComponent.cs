using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class QueenEvolutionComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> QueenPolymorphPrototype = "AlienEvolutionQueen";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? QueenEvolutionAction = "ActionEvolveQueen";

    [DataField]
    public EntityUid? QueenEvolutionActionEntity;

    [DataField]
    public float PlasmaCost = 490f;
}
