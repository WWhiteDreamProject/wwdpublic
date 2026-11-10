using System.Linq;
using Content.Shared._White.Teleportation.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._White.Teleportation.Systems;

public sealed class WhitePortalSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<WhitePortalComponent> _portalQuery;

    private const string PortalFixture = "portalFixture";
    private const string ProjectileFixture = "projectile";

    private static readonly EntProtoId PortalPrototype = "Portal";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhitePortalComponent, StartCollideEvent>(OnStartCollide);

        _portalQuery = GetEntityQuery<WhitePortalComponent>();
    }

    private void OnStartCollide(Entity<WhitePortalComponent> ent, ref StartCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixtureId, args.OtherFixtureId, args.OtherFixture))
            return;

        var transform = Transform(args.OtherEntity);
        if (transform.Anchored)
            return;

        RaiseLocalEvent(args.OtherEntity, new BeforeTeleportationEvent());

        _audio.PlayPredicted(ent.Comp.EnteringSound, ent, args.OtherEntity);

        if (ent.Comp.Coordinates.HasValue)
        {
            _transform.SetCoordinates(args.OtherEntity, transform, ent.Comp.Coordinates.Value);
            return;
        }

        if (_net.IsClient) // TODO: Use PredictedRandom
            return;

        var activeMaps = new List<MapId>();
        foreach (var map in _map.GetAllMapIds())
        {
            if (_map.IsPaused(map))
                continue;

            activeMaps.Add(map);
        }

        var newMap = _random.Pick(activeMaps);
        var newCoordinates = _random.NextVector2(ent.Comp.MaxRandomDistance);

        _transform.SetMapCoordinates((args.OtherEntity, transform), new (newCoordinates, newMap));
    }

    private bool ShouldCollide(string ourId, string otherId, Fixture other) =>
        // most non-hard fixtures shouldn't pass through portals, but projectiles are non-hard as well
        // and they should still pass through
        ourId == PortalFixture && (other.Hard || otherId == ProjectileFixture);

    public void SpawnPortal(EntityCoordinates coordinates, EntityCoordinates? teleportTo = null)
    {
        var portal = PredictedSpawnAtPosition(PortalPrototype, coordinates);
        if (_portalQuery.TryComp(portal, out var portalComponent))
        {
            portalComponent.Coordinates = teleportTo;
            Dirty(portal, portalComponent);
            return;
        }

        Log.Error($"Fail spawn portal without {nameof(WhitePortalComponent)}, prototype: {PortalPrototype}");
        PredictedQueueDel(portal);
    }
}

public record struct BeforeTeleportationEvent;
