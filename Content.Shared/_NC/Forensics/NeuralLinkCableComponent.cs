using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Forensics;

[RegisterComponent, NetworkedComponent]
public sealed partial class NeuralLinkCableComponent : Component
{
    [DataField("durationSeconds")]
    public float DurationSeconds = 12f;

    [DataField("maxDeathMinutes")]
    public float MaxDeathMinutes = 15f;

    [DataField("maxLogLines")]
    public int MaxLogLines = 5;
}


