using Robust.Shared.Audio;


namespace Content.Shared._White.Bark.Components;


[RegisterComponent]
public sealed partial class BarkComponent : Component
{
    [DataField] public SoundSpecifier BarkSound { get; set; }
    [DataField] public float PauseAverage = 0.095f;
    [DataField] public float PitchAverage = 1f;
    [DataField] public float PitchVariance = 0.1f;
    [DataField] public float VolumeAverage = 0f;
}
