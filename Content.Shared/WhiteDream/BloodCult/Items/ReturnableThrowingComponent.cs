using Robust.Shared.GameStates;

namespace Content.Shared.WhiteDream.BloodCult.Items;

/// <summary>
/// Component for items that are returned to the owner after being thrown
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReturnableThrowingComponent : Component
{
    /// <summary>
    /// The last owner of the item to whom the item should be returned
    /// </summary>
    [DataField]
    public EntityUid? LastThrower;
} 