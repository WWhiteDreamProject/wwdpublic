using Robust.Shared.Prototypes;

namespace Content.Shared._White.Abilities.Psionics;

[RegisterComponent]
public sealed partial class PsionicCloneComponent : Component 
{
    [ViewVariables]
    public EntityUid? OriginalUid;

    [DataField]
    public ProtoId<EntityPrototype> SpawnOnDeathPrototype { get; set; } = "Ectoplasm";
}