using Content.Shared._White.Nutrition.Systems;

namespace Content.Shared._White.Nutrition.Components;

/// <summary>
/// Component that denotes a piece of clothing that blocks the mouth or otherwise prevents eating & drinking.
/// </summary>
[RegisterComponent, Access(typeof(SharedIngestionSystem))]
public sealed partial class IngestionBlockerComponent : Component
{
    /// <summary>
    /// Is this component currently blocking consumption?
    /// </summary>
    [DataField]
    public bool Enabled = true;
}
