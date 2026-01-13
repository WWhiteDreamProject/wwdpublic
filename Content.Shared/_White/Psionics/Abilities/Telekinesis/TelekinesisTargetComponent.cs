using Robust.Shared.GameStates;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class TelekinesisTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Tetherer;

    [DataField]
    public float OriginalAngularDamping;

    [DataField]
    public float OriginalLinearDamping;
}
