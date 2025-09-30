using Robust.Shared.Prototypes;

namespace Content.Shared._White.BloodCult.Construct;

[RegisterComponent]
public sealed partial class ConstructComponent : Component
{
    [DataField]
    public ProtoId<EntityPrototype> SpawnOnDeathPrototype { get; set; } = "Ectoplasm";
}
