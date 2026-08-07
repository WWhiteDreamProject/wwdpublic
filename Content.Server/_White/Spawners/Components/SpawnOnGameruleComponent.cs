using Robust.Shared.Prototypes;

namespace Content.Server._White.Spawners.Components;

[RegisterComponent]
public sealed partial class SpawnOnGameruleComponent : Component
{
    [DataField("spawnPrototype")]
    public string? SpawnPrototype;
}
