using Content.Shared.Chemistry.Components;

namespace Content.Shared._White.Nutrition.Components;

[RegisterComponent]
public sealed partial class IngestionProviderComponent : Component
{
    /// <summary>
    /// The name of the solution inside of this provider.
    /// </summary>
    [DataField]
    public string SolutionName = "ingestion-provider";

    /// <summary>
    /// The maximum capacity of the provider solution.
    /// </summary>
    [DataField]
    public float SolutionMaxVolume = 150f;

    /// <summary>
    /// The solution inside of this provider.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;
}
