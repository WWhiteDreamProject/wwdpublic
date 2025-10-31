using Robust.Shared.Prototypes;

namespace Content.Shared._Friday31.Pennywise;

[RegisterComponent]
public sealed partial class PennywiseAbilityComponent : Component
{
    [DataField]
    public EntProtoId ChameleonAction = "ActionPennywiseChameleon";

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ChameleonActionEntity;

    [DataField]
    public EntProtoId PhaseToggleAction = "ActionPennywisePhaseToggle";

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? PhaseToggleActionEntity;

    [DataField]
    public EntProtoId SpawnBalloonAction = "ActionPennywiseSpawnBalloon";

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? SpawnBalloonActionEntity;

    [DataField]
    public List<EntProtoId> BalloonPrototypes = new()
    {
        "BalloonSyn"
    };

    [DataField]
    public float BalloonCooldown = 5f;

    public TimeSpan LastBalloonSpawn = TimeSpan.Zero;
}
