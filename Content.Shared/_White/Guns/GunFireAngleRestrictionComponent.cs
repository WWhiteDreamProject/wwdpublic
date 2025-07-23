using Robust.Shared.GameStates;

namespace Content.Shared._White.Guns;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class GunFireAngleRestrictionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}

