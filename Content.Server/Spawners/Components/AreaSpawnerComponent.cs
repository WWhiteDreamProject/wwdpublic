using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Spawners.Components; // WWDP SYSTEM

/// <summary>
/// Spawns Entities in area around spawner
/// </summary>
[RegisterComponent]
public sealed partial class AreaSpawnerComponent : Component
{
    // Maximum offset of entities spawned.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Radius;

    // Prototype of entity spawned
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public EntProtoId SpawnPrototype;

    /// <summary>
    /// Length of the interval between spawn attempts.
    /// </summary>
    [DataField]
    public int IntervalSeconds = 20;

    // This will spawn entities to every tile in spawn radius
    [DataField]
    public bool SpawnToAllValidTiles = true;

    [ViewVariables]
    public int SpawnRadius;

    public CancellationTokenSource? TokenSource;
}
