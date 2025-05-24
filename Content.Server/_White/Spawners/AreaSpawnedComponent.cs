namespace Content.Server._White.Spawners;

[RegisterComponent]
public sealed partial class AreaSpawnedComponent : Component
{
    [ViewVariables]
    public EntityUid Spawner;
}
