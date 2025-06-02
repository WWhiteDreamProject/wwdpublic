using Content.Shared.Whitelist;

namespace Content.Server._White.Throwing;

[RegisterComponent]
public sealed partial class ReturnItemOnThrowComponent : Component
{
    /// <summary>
    ///     Return speed of the item to its owner
    /// </summary>
    [DataField]
    public float ReturnSpeed = 15;

    /// <summary>
    ///     Entities included in thrower's check.
    /// </summary>
    [DataField]
    public EntityWhitelist? ThrowerWhitelist;

    /// <summary>
    ///     Entities included in collision check.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    /// <summary>
    ///     Entities excluded from collision check.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetBlacklist;

    /// <summary>
    /// The entity to which the item is currently returning
    /// </summary>
    [ViewVariables]
    public EntityUid? ReturningTo;
} 