using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Aliens.Components;

/// <summary>
/// The QueenEvolutionComponent is used to manage the evolution behavior of the alien queen.
/// </summary>
[RegisterComponent]
public sealed partial class QueenEvolutionComponent : Component
{
    /// <summary>
    /// Prototype ID for the polymorph effect associated with the queen evolution.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype> QueenPolymorphPrototype = "AlienEvolutionQueen";

    /// <summary>
    /// Action prototype ID for the evolution action.
    /// </summary>
    [DataField]
    public EntProtoId? QueenEvolutionAction = "ActionEvolveQueen";

    /// <summary>
    /// Reference to the entity associated with the evolution action.
    /// </summary>
    [DataField]
    public EntityUid? QueenEvolutionActionEntity;

    /// <summary>
    /// Reference to the entity associated with the evolution action.
    /// </summary>
    [DataField]
    public float PlasmaCost = 490f;
}
