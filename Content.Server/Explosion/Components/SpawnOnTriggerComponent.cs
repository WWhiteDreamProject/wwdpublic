using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Numerics;

namespace Content.Server.Explosion.Components;

[RegisterComponent, Access(typeof(TriggerSystem))]
public sealed partial class SpawnOnTriggerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Proto = string.Empty;

    // WWDP EDIT START
    [DataField]
    public List<Vector2> Offsets = new() { Vector2.Zero };
    // WWDP EDIT END
}
