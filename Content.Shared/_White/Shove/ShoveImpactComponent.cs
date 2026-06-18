using Robust.Shared.GameStates;

namespace Content.Shared._White.Shove;

/// <summary>
/// Stores the original intended shove speed before it was capped by wall raycast.
/// Used to calculate wall impact damage based on the original force.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShoveImpactComponent : Component
{
    /// <summary>
    /// The original throw speed before raycast capping.
    /// </summary>
    [DataField]
    public float OriginalSpeed;
}