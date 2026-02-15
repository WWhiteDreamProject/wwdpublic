using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Particles.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, EntityCategory("Particles")]
public sealed partial class ParticleComponent : Component
{
    /// <summary>
    /// Optional prototype to spawn upon landing.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnOnLand;

    /// <summary>
    /// A multiplier applied to the <see cref="FlyTime"/> to randomize the particle flight duration.
    /// </summary>
    [DataField]
    public float FlyTimeMultiplier = 1.2f;

    /// <summary>
    /// The sound specifier played when this particle lands or impacts.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnLand;

    /// <summary>
    /// The flight duration for the particle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FlyTime = TimeSpan.FromSeconds(0.5f);

    [ViewVariables]
    public TimeSpan FlyTimeEnd = TimeSpan.Zero;
}
