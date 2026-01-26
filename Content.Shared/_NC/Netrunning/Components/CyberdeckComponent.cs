using Robust.Shared.GameStates;

namespace Content.Shared._NC.Netrunning.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CyberdeckComponent : Component
{
    /// <summary>
    /// Maximum RAM capacity of the deck.
    /// </summary>
    [DataField("maxRam"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxRam = 10;

    /// <summary>
    /// Current available RAM.
    /// </summary>
    [DataField("currentRam"), ViewVariables(VVAccess.ReadWrite)]
    public int CurrentRam = 10;

    /// <summary>
    /// RAM recovery per second.
    /// </summary>
    [DataField("recoverySpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float RecoverySpeed = 1.0f;

    /// <summary>
    /// Netrunning range (in tiles/meters).
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 10.0f;

    /// <summary>
    /// Accumulator for passive RAM regeneration.
    /// </summary>
    public float RecoveryAccumulator = 0f;
}
