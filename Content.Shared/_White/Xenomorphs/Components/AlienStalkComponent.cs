using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AlienStalkComponent : Component
{

    [DataField]
    public EntProtoId? StalkAction = "ActionStalkAlien";

    [DataField]
    public EntityUid? StalkActionEntity;

    [DataField]
    public int PlasmaCost = 5;

    public bool IsActive;

    public float Sprint;
}
