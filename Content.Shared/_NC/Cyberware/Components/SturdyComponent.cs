// D:\projects\night-station\Content.Shared\_NC\Cyberware\Components\SturdyComponent.cs
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Makes an entity resistant to knockdown, stunning, and knockback.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SturdyComponent : Component
{
    /// <summary>
    ///     If true, the entity cannot be knocked down.
    /// </summary>
    [DataField("knockdownImmunity")]
    public bool KnockdownImmunity = true;

    /// <summary>
    ///     If true, the entity cannot be stunned.
    /// </summary>
    [DataField("stunImmunity")]
    public bool StunImmunity = true;

    /// <summary>
    ///     If true, the entity cannot be knocked back by impulses.
    /// </summary>
    [DataField("knockbackImmunity")]
    public bool KnockbackImmunity = true;

    /// <summary>
    ///     Stores the original body type to restore it when immunity is removed.
    /// </summary>
    [ViewVariables]
    public BodyType BaseBodyType = BodyType.Dynamic;
}
