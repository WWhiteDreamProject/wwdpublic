using Robust.Shared.Audio;
using Robust.Shared.GameStates;


namespace Content.Shared._White.Bark.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, ]
public sealed partial class BarkComponent : Component
{
    [DataField, AutoNetworkedField] public SoundSpecifier BarkSound { get; set; }
    [DataField, AutoNetworkedField] public float PauseAverage = 0.095f;
    [DataField, AutoNetworkedField] public float PitchAverage = 1f;
    [DataField, AutoNetworkedField] public float PitchVariance = 0.1f;
    [DataField, AutoNetworkedField] public float VolumeAverage = 0f;
}
