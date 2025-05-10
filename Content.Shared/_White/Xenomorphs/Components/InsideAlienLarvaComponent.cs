using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class InsideAlienLarvaComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphPrototype = "AlienEvolutionGrowStageTwo";

    [DataField]
    public EntProtoId? EvolutionAction = "ActionLarvaGrow";

    [DataField]
    public EntityUid? EvolutionActionEntity;

    [DataField]
    public TimeSpan EvolutionCooldown = TimeSpan.Zero;

    public bool IsGrown;
}

public sealed partial class AlienLarvaGrowActionEvent : InstantActionEvent { }
