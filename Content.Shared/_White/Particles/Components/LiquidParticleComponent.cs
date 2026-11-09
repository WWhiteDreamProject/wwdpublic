namespace Content.Shared._White.Particles.Components;

[RegisterComponent]
public sealed partial class LiquidParticleComponent : Component
{
    /// <summary>
    /// The name of the solution associated with the particle when it is emitted.
    /// </summary>
    [DataField]
    public string InitialSolutionName = "particle";

    /// <summary>
    /// The name of the solution associated with the particle when it impacts or lands.
    /// </summary>
    [DataField]
    public string LandSolutionName = "particle";
}
