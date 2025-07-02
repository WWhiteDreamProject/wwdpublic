namespace Content.Server.Atmos.Components;

/// <summary>
/// When present on an entity, triggers the fixgridatmos command for its grid when the round starts.
/// Useful for ensuring standard atmosphere on round start during testing or mapping.
/// </summary>
[RegisterComponent]
public sealed partial class FixGridAtmosOnRoundStartComponent : Component
{
}
