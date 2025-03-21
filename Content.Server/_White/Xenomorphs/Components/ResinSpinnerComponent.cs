using Robust.Shared.Prototypes;

namespace Content.Server._White.Xenomorphs.Components;

[RegisterComponent]
public sealed partial class ResinSpinnerComponent : Component
{
    [DataField]
    public EntProtoId? ResinWallAction = "ActionSpawnAlienWall";

    [DataField]
    public EntityUid? ResinWallActionEntity;

    [DataField]
    public EntProtoId? ResinWindowAction = "ActionSpawnAlienWindow";

    [DataField]
    public EntityUid? ResinWindowActionEntity;

    [DataField]
    public EntProtoId? NestAction = "ActionSpawnAlienNest";

    [DataField]
    public EntityUid? NestActionEntity;
}
