using Robust.Shared.GameStates;

namespace Content.Shared._White.Animations;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlipOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ApplyToSelf = true;
}
