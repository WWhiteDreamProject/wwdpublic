namespace Content.Server.Chemistry.Components;

/// <summary>
/// Used for embeddable entities that should try to inject a
/// contained solution into a target when they become embedded in it.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionInjectOnEmbedComponent : BaseSolutionInjectOnEventComponent
{
    /// <summary>
    /// Used to override the PierceArmor setting when fired from a SyringeGun.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool? PierceArmorOverride;
}
