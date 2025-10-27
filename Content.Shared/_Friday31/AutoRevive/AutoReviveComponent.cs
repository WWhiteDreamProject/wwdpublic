using Robust.Shared.GameStates;

namespace Content.Shared._Friday31.AutoRevive;

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoReviveComponent : Component
{
    [DataField]
    public float ReviveDelay = 3f;

    [DataField]
    public TimeSpan? DeathTime;
}
