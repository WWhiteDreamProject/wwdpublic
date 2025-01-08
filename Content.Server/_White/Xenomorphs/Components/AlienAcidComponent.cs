using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Aliens.Components;

/// <summary>
/// The AlienAcidComponent is used for managing alien acid behavior and properties.
/// </summary>
[RegisterComponent]
public sealed partial class AlienAcidComponent : Component
{
    /// <summary>
    // Prototype ID for the acid.
    /// </summary>
    [DataField]
    public EntProtoId AcidPrototype = "CorrosiveAcidOverlay";

    /// <summary>
    // Duration for the melting effect.
    /// </summary>
    [ViewVariables]
    public TimeSpan MeltTimeSpan = TimeSpan.Zero;

    /// <summary>
    // Time required for the acid to melt through an entity, in seconds.
    /// </summary>
    [DataField]
    public int MeltTime = 30;

    /// <summary>
    // Reference to the wall entity affected by the acid.
    /// </summary>
    [ViewVariables]
    public EntityUid? WallUid;
}
