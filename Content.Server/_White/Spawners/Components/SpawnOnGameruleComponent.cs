using Robust.Shared.Prototypes;

namespace Content.Shared._White.SpawnOnGamerule.Components;

[RegisterComponent]
public sealed partial class SpawnOnGameruleComponent : Component
{
    [DataField("spawnPrototype")]
    public string? SpawnPrototype;
}
