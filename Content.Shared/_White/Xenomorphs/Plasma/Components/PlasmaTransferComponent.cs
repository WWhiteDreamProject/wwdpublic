using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Plasma.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PlasmaTransferComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionAlienTransferPlasma";

    [DataField]
    public float Amount = 50f;

    [ViewVariables]
    public EntityUid? ActionEntity;
}

public sealed partial class TransferPlasmaActionEvent : EntityTargetActionEvent { }
