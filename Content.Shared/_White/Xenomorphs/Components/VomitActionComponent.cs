using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VomitActionComponent : Component
{
    [DataField]
    public EntProtoId? VomitAction = "ActionVomit";

    [DataField]
    public EntityUid? VomitActionEntity;

    [DataField]
    public float ThirstAdded = 40f;

    [DataField]
    public float HungerAdded = 40f;

    public Container Stomach = default!;
}
