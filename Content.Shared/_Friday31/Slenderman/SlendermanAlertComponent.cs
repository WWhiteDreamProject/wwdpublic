using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Friday31.Slenderman;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlendermanAlertComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionSlendermanAlert";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool IsActive;

    [DataField]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/_Friday31/slender/slenderman-sound-cut.ogg");

    [DataField]
    public float MaxDistance = 30f;

    [DataField]
    public float ReferenceDistance = 5f;

    [DataField]
    public EntityUid? SoundStream;
}
