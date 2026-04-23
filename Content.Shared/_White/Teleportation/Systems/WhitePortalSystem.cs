using System.Linq;
using System.Numerics;
using Content.Shared._White.Teleportation.Components;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
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
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

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
            var oldMapVelocity = Vector2.Zero;
            PhysicsComponent? body = null;
            if (TryComp<PhysicsComponent>(args.OtherEntity, out body))
                oldMapVelocity = _physics.GetMapLinearVelocity(args.OtherEntity, body);

            _transform.SetCoordinates(args.OtherEntity, transform, ent.Comp.Coordinates.Value);

            if (body != null)
            {
                var newXform = Transform(args.OtherEntity);
                var newGridVelocity = Vector2.Zero;
                if (newXform.GridUid != null && TryComp<PhysicsComponent>(newXform.GridUid.Value, out var gridBody))
                    newGridVelocity = gridBody.LinearVelocity;

                var relativeSpeed = (oldMapVelocity - newGridVelocity).Length();
                if (_net.IsServer && relativeSpeed >= 20f && HasComp<DamageableComponent>(args.OtherEntity))
                {
                    var damage = new DamageSpecifier();
                    damage.DamageDict.Add("Blunt", 5f * (relativeSpeed / 10f));
                    _damageable.TryChangeDamage(args.OtherEntity, damage);
                    _stun.TryParalyze(args.OtherEntity, TimeSpan.FromSeconds(2), true);
                }
                var currentVelocity = body.LinearVelocity;
                const float maxSafeSpeed = 15f; // TODO: MB add in CVars (2)
                if (currentVelocity.Length() > maxSafeSpeed)
                    _physics.SetLinearVelocity(args.OtherEntity, currentVelocity.Normalized() * maxSafeSpeed, body: body);
            }
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
