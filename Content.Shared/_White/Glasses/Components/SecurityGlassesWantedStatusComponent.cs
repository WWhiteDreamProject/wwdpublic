using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Glasses.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SecurityGlassesWantedStatusComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class SecurityGlassesWantedStatusComponentState : ComponentState
{
}

[NetSerializable, Serializable]
public enum SecurityGlassesWantedStatusVisuals
{
    Active
} 