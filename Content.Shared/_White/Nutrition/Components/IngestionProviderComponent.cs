using Content.Shared.Chemistry.Components;

namespace Content.Shared._White.Nutrition.Components;

[RegisterComponent]
public sealed partial class IngestionProviderComponent : Component
{
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
