using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// NC: Allows modifying the sell price of an entity on the cargo pallet.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CargoSellModifierComponent : Component
{
    /// <summary>
    /// Multiplier applied to the calculated price.
    /// </summary>
    [DataField("multiplier")]
    public float Multiplier = 1.0f;
}
