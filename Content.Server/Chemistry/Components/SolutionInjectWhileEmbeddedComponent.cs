namespace Content.Server.Chemistry.Components;

/// <summary>
/// Used for embeddable entities that should try to inject their
/// contained solution into the entity they are embedded in over time.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionInjectWhileEmbeddedComponent : BaseSolutionInjectOnEventComponent
{
    /// <summary>
    /// The interval between injection attempts, in seconds.
    /// </summary>
    [DataField]
    public float UpdateInterval = 3.0f;

    /// <summary>
    /// Maximum number of injections that can be performed before the component removes itself.
    /// Null means unlimited.
    /// </summary>
    [DataField]
    public int? Injections = 5;

    /// <summary>
    /// Used to override the PierceArmor setting when fired from a SyringeGun.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool? PierceArmorOverride;

    /// <summary>
    /// Used to speed up injections when fired from a SyringeGun.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float SpeedMultiplier = 1f;
} 