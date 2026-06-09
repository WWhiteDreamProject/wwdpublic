using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.BloodCult.Construct;

[RegisterComponent]
public sealed partial class ConstructComponent : Component
{
    [DataField]
    public ProtoId<EntityPrototype> SpawnOnDeathPrototype { get; set; } = "Ectoplasm";
}

[Serializable, NetSerializable]
public enum ConstructLayer : byte
{
    Base,
    Unshaded
}
