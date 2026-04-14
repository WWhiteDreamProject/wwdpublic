using Robust.Shared.GameStates;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Prevents movement speed from dropping below 100% (1.0), 
///     while still allowing speed buffs above 100%.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberwareSlowImmunityComponent : Component
{
}
