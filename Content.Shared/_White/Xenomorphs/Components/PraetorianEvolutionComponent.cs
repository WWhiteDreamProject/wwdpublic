using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PraetorianEvolutionComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> PraetorianPolymorphPrototype = "AlienEvolutionPraetorian";

    [DataField]
    public EntProtoId? PraetorianEvolutionAction = "ActionEvolvePraetorian";

    [DataField]
    public EntityUid? PraetorianEvolutionActionEntity;

    [DataField]
    public float PlasmaCost = 490f;
}
