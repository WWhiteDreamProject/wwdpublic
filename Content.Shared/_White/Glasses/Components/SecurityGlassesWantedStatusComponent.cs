using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Glasses.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SecurityGlassesWantedStatusComponent : Component
{
    [DataField("cooldownTime")]
    public float CooldownTime { get; set; } = 1.0f;
    
    [DataField("maxDistance")]
    public float MaxDistance { get; set; } = 5.0f;
}

[Serializable, NetSerializable]
public sealed class SecurityGlassesWantedStatusComponentState : ComponentState
{
    public float NextUseTime { get; }
    
    public SecurityGlassesWantedStatusComponentState(float nextUseTime)
    {
        NextUseTime = nextUseTime;
    }
}

[NetSerializable, Serializable]
public enum SecurityGlassesWantedStatusVisuals
{
    Active,

    Scanning
} 