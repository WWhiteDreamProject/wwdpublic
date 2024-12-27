using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AlienStalkComponent : Component
{

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StalkAction = "ActionStalkAlien";

    [DataField]
    public EntityUid? StalkActionEntity;

    [DataField]
    public int PlasmaCost = 5;

    public bool IsActive;

    public float Sprint;
}
