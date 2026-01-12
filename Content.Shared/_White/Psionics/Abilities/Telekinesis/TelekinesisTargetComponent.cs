using Robust.Shared.GameStates;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class TelekinesisTargetComponent : Component
{
    [DataField]
    public EntityUid Tetherer;

    [DataField]
    public float OriginalAngularDamping;
}
