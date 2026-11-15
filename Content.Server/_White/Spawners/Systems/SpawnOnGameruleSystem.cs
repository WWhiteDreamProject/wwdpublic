using Content.Server._White.Spawners.Components;
using Content.Server._White.Spawners.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._White.Spawners.Systems;

public sealed class SpawnOnGameruleSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnOnGameruleComponent, ActivateMarkerEvent>(OnActivateMarker);
    }

    private void OnActivateMarker(EntityUid uid, SpawnOnGameruleComponent component, ActivateMarkerEvent args)
    {
        if (component.SpawnPrototype != null)
        {
            var coords = Transform(uid).Coordinates;
            EntityManager.SpawnEntity(component.SpawnPrototype, coords);
        }
        EntityManager.QueueDeleteEntity(uid);
    }
}
