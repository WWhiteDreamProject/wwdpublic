using Robust.Shared.Prototypes;

namespace Content.Server._White.Spawners;

[RegisterComponent]
public sealed partial class AreaSpawnerComponent : Component
{
    [DataField]
    public int Radius = 3;

    [DataField]
    public EntProtoId SpawnPrototype;

    [DataField]
    public TimeSpan SpawnDelay = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan SpawnAt = TimeSpan.Zero;
}
