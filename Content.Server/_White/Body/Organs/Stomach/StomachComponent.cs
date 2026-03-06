using Content.Shared.Chemistry.Components;
using Content.Shared.Whitelist;

namespace Content.Server._White.Body.Organs.Stomach;

[RegisterComponent, Access(typeof(StomachSystem))]
public sealed partial class StomachComponent : Component
{
    /// <summary>
    /// Controls whitelist behavior. If true, this stomach can digest <i>only</i> food that passes the whitelist. If false, it can digest normal food <i>and</i> any food that passes the whitelist.
    /// </summary>
    [DataField]
    public bool IsSpecialDigestibleExclusive = true;

    /// <summary>
    /// A whitelist for what special-digestible-required foods this stomach is capable of eating.
    /// </summary>
    [DataField]
    public EntityWhitelist? SpecialDigestible;

    /// <summary>
    /// The name of the solution inside of this stomach.
    /// </summary>
    [DataField]
    public string? SolutionName = "stomach";

    /// <summary>
    /// The solution inside of this stomach.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;
}
