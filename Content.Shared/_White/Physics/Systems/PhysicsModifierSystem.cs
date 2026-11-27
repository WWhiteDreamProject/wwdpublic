using Content.Shared._White.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._White.Physics.Systems;

/// <summary>
/// Lets several systems modify the collision mask and layer of an entity's fixtures at once.
/// </summary>
public sealed class PhysicsModifierSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<PhysicsModifierComponent> _physicsModifierQuery;

    public override void Initialize()
    {
        base.Initialize();

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _physicsModifierQuery = GetEntityQuery<PhysicsModifierComponent>();
    }

    /// <summary>
    /// Recalculates the collision mask and layer of every fixture of the entity.
    /// </summary>
    public void RefreshCollision(Entity<FixturesComponent?, PhysicsModifierComponent?> ent)
    {
        if (!_fixturesQuery.Resolve(ent, ref ent.Comp1, false))
            return;

        _physicsModifierQuery.Resolve(ent, ref ent.Comp2, false);

        Dictionary<string, FixtureCollisionModification> modifications = new();
        foreach (var (id, fixture) in ent.Comp1.Fixtures)
        {
            var previous = ent.Comp2?.Fixtures.GetValueOrDefault(id) ?? default;

            // Undo the previous modification to get the values as they are without it.
            var baseMask = (fixture.CollisionMask | previous.RemovedMask) & ~previous.AddedMask;
            var baseLayer = (fixture.CollisionLayer | previous.RemovedLayer) & ~previous.AddedLayer;

            var ev = new UpdateCollisionEvent(id, fixture, baseMask, baseLayer);
            RaiseLocalEvent(ent, ref ev);

            if (ev.Mask != fixture.CollisionMask)
                _physics.SetCollisionMask(ent, id, fixture, ev.Mask, ent.Comp1);

            if (ev.Layer != fixture.CollisionLayer)
                _physics.SetCollisionLayer(ent, id, fixture, ev.Layer, ent.Comp1);

            var modification = new FixtureCollisionModification
            {
                RemovedMask = baseMask & ~ev.Mask,
                AddedMask = ev.Mask & ~baseMask,
                RemovedLayer = baseLayer & ~ev.Layer,
                AddedLayer = ev.Layer & ~baseLayer
            };

            if (modification.IsEmpty)
                continue;

            modifications[id] = modification;
        }

        // Nothing is modified anymore: forget the entity, so it costs nothing.
        if (modifications.Count == 0)
        {
            if (ent.Comp2 is not null)
                RemComp(ent, ent.Comp2);

            return;
        }

        var comp = ent.Comp2 ?? EnsureComp<PhysicsModifierComponent>(ent);
        comp.Fixtures = modifications;
        Dirty(ent, comp);
    }
}

/// <summary>
/// Raised on an entity for each of its fixtures when its collision is refreshed by <see cref="PhysicsModifierSystem"/>.
/// Handlers change <see cref="Mask"/> and <see cref="Layer"/> according to their own state.
/// </summary>
/// <param name="FixtureId">The ID of the fixture.</param>
/// <param name="Fixture">The fixture itself.</param>
/// <param name="Mask">The collision mask of the fixture. Starts as the unmodified mask.</param>
/// <param name="Layer">The collision layer of the fixture. Starts as the unmodified layer.</param>
[ByRefEvent]
public record struct UpdateCollisionEvent(string FixtureId, Fixture Fixture, int Mask, int Layer);
