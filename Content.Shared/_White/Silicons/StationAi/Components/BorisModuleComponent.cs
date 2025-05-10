using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Silicons.StationAi.Components;

[RegisterComponent]
public sealed partial class BorisModuleComponent : Component
{
    [ViewVariables]
    public EntityUid? OriginalBrain;

    [DataField]
    public EntProtoId ReturnToCoreActionProtoId = "ActionAIReturnToCore";

    public EntityUid? ReturnToCoreActionContainerId;
}

public sealed partial class AiReturnToCoreEvent: InstantActionEvent;
