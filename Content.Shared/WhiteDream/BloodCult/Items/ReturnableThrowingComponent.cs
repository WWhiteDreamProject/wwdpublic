using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.WhiteDream.BloodCult.Items;

/// <summary>
/// Component for items that can return to their owner after being thrown.
/// Currently used for the cult's mirror shield.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReturnableThrowingComponent : Component
{
    /// <summary>
    /// The last player who threw this item
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? LastThrower;
    
    /// <summary>
    /// Flag indicating that the item is currently returning to its owner
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IsReturning = false;
    
    /// <summary>
    /// The entity to which the item is currently returning
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? TargetEntity = null;
} 