using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AlienAcidComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string AcidPrototype = "CorrosiveAcidOverlay";

    [ViewVariables]
    public TimeSpan MeltTimeSpan = TimeSpan.Zero;

    [DataField]
    public int MeltTime = 30;

    [ViewVariables]
    public EntityUid? WallUid;
}
