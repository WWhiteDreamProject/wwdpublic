using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Content.Shared.RadialSelector;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlienEvolutionComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> EvolvesTo = new();

    [DataField, AutoNetworkedField]
    public TimeSpan EvolutionDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public FixedPoint2 Points;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Max;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PointsPerSecond = 0.5;

    [DataField, AutoNetworkedField]
    public TimeSpan EvolutionJitterDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public EntProtoId<InstantActionComponent> EvolutionActionId = "ActionEvolution";

    [ViewVariables]
    public EntityUid? EvolutionAction;

    [ViewVariables]
    public TimeSpan LastPointsAt;
}
