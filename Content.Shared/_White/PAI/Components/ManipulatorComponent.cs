using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared._White.PAI.Components;

[RegisterComponent]
public sealed partial class ManipulatorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsActive = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsGrabbin = false;

    [DataField, ViewVariables]
    public EntProtoId ManipulatorProto = "PAImanipulator";

    [DataField, ViewVariables]
    public EntityUid? Manipulator;

    [DataField, ViewVariables]
    public EntityUid? GrabbedEntity;

    [DataField, ViewVariables]
    public float ManipulatorSpeed = 5f;

    [DataField]
    public bool IsReturning = false;

    [DataField]
    public MapCoordinates? TargetWorldPos;

    [DataField, ViewVariables]
    public SpriteSpecifier JointSpite =
        new SpriteSpecifier.Rsi(new ResPath("_White/Objects/Specific/pai_manipulator.rsi"), "rope");
}
