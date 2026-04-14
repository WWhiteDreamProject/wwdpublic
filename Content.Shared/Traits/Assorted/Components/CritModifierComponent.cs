using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted.Components;

/// <summary>
///     This is used for any trait that modifies Crit/Dead Thresholds
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CritModifierComponent : Component
{
    /// <summary>
    ///     The amount that an entity's critical threshold will be incremented by.
    /// </summary>
    [DataField("critThresholdModifier"), AutoNetworkedField]
    public int CritThresholdModifier { get; set; } = 0;

    /// <summary>
    ///     The amount that an entity's dead threshold will be incremented by.
    /// </summary>
    [DataField("deadThresholdModifier"), AutoNetworkedField]
    public int DeadThresholdModifier { get; set; } = 0;
}
