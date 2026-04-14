using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Stores base stats of an entity before cyberware modifiers are applied.
///     Prevents stats from stacking infinitely when implants are refreshed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberwareStatsComponent : Component
{
    /// <summary>
    ///     Base health thresholds (Critical, Dead, etc.)
    /// </summary>
    [DataField("baseThresholds"), AutoNetworkedField]
    public Dictionary<MobState, FixedPoint2> BaseThresholds = new();

    /// <summary>
    ///     Whether base stats have been captured.
    /// </summary>
    [DataField("captured"), AutoNetworkedField]
    public bool Captured = false;
}
