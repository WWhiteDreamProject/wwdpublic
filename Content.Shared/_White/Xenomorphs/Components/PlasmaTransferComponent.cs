using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlasmaTransferComponent : Component
{
    public EntProtoId Action = "ActionAlienTransferPlasma";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public float Amount = 50f;
}

public sealed partial class TransferPlasmaActionEvent : EntityTargetActionEvent { }
