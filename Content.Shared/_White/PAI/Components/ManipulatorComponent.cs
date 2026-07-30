using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.PAI.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ManipulatorComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsActive = false;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsGrabbing = false;

    [DataField]
    public EntProtoId ManipulatorProto = "PAImanipulator";

    [DataField, AutoNetworkedField]
    public EntityUid? Manipulator;

    [DataField, AutoNetworkedField]
    public EntityUid? GrabbedEntity;

    [DataField]
    public float ManipulatorSpeed = 5f;

    [DataField, AutoNetworkedField]
    public bool IsReturning = false;

    [DataField]
    public MapCoordinates? TargetWorldPos;

    [DataField, ViewVariables]
    public SpriteSpecifier JointSpite =
        new SpriteSpecifier.Rsi(new ResPath("_White/Objects/Specific/pai_manipulator.rsi"), "rope");
}
