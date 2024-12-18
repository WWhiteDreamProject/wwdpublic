using Robust.Shared.Audio;

namespace Content.Server.Anomaly.Components;

[RegisterComponent]
public sealed partial class WormholeAnomalyComponent : Component
{
    /// <summary>
    /// The pulse interval in seconds.
    /// </summary>
    [DataField("pulseInterval")]
    public float PulseInterval = 15f;

    /// <summary>
    /// The maximum shuffle distance.
    /// </summary>
    [DataField("maxShuffleRadius")]
    public float MaxShuffleRadius = 40f;

    /// <summary>
    /// The sound after shuffled around.
    /// </summary>
    [DataField("teleportSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
