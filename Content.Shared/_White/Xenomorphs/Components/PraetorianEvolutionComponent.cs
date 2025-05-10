using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

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
