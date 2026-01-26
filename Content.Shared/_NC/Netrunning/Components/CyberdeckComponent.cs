using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning.Components;

[Serializable, NetSerializable]
public enum CyberdeckUiKey : byte
{
    Key
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberdeckComponent : Component
{
    /// <summary>
    /// Maximum RAM capacity of the deck.
    /// </summary>
    [DataField("maxRam"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int MaxRam = 10;

    /// <summary>
    /// Current available RAM.
    /// </summary>
    [DataField("currentRam"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int CurrentRam = 10;

    /// <summary>
    /// RAM recovery per second.
    /// </summary>
    [DataField("recoverySpeed"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float RecoverySpeed = 1.0f;

    /// <summary>
    /// Netrunning range (in tiles/meters).
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Range = 10.0f;

    /// <summary>
    /// The currently selected target for hacks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActiveTarget;

    /// <summary>
    /// Accumulator for passive RAM regeneration.
    /// </summary>
    public float RecoveryAccumulator = 0f;
}
