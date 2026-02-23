using System.Numerics;
using Content.Shared._White.Particles.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._White.Particles.Systems;

public sealed partial class ParticleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<LiquidParticleComponent> _liquidParticleQuery;
    private EntityQuery<ParticleComponent> _particleQuery;

    private readonly HashSet<Entity<ParticleComponent>> _particles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleComponent, MapInitEvent>(OnMapInit, after: new []{typeof(SharedSolutionContainerSystem), });
        SubscribeLocalEvent<ParticleComponent, ComponentRemove>(OnRemove);

        _liquidParticleQuery = GetEntityQuery<LiquidParticleComponent>();
        _particleQuery = GetEntityQuery<ParticleComponent>();

        InitializeLiquid();
    }

    private void OnMapInit(Entity<ParticleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.FlyTime *= _random.NextFloat(1 / ent.Comp.FlyTimeMultiplier, ent.Comp.FlyTimeMultiplier);
        ent.Comp.FlyTimeEnd = _gameTiming.CurTime + ent.Comp.FlyTime;
        Dirty(ent);

        _particles.Add(ent);
    }

    private void OnRemove(Entity<ParticleComponent> ent, ref ComponentRemove args) =>
        _particles.Remove(ent);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _particles)
        {
            if (_gameTiming.CurTime < ent.Comp.FlyTimeEnd)
                continue;

            var coordinates = _transform.GetMoverCoordinates(ent);
            _audio.PlayPvs(ent.Comp.SoundOnLand, coordinates);

            if (!ent.Comp.SpawnOnLand.HasValue)
            {
                QueueDel(ent);
                continue;
            }

            var landedParticle = Spawn(ent.Comp.SpawnOnLand.Value, coordinates);
            _transform.SetWorldRotation(landedParticle, _transform.GetWorldRotation(ent));

            var ev = new AfterParticleLandedEvent(landedParticle);
            RaiseLocalEvent(ent, ev);

            QueueDel(ent);
        }
    }

    public Entity<ParticleComponent>? SpawnParticle(
        EntProtoId prototype,
        MapCoordinates coordinates,
        Vector2 direction,
        float distance
        )
    {
        var particleUid = Spawn(prototype, coordinates);

        if (!_particleQuery.TryComp(particleUid, out var particleComponent))
        {
            Log.Error($"Fail spawn particle without {nameof(ParticleComponent)}, prototype: {prototype}");
            QueueDel(particleUid);
            return null;
        }

        var impulse = direction * (distance / (float) particleComponent.FlyTime.TotalSeconds);
        _physics.ApplyLinearImpulse(particleUid, impulse);

        return (particleUid, particleComponent);
    }
}

public record struct AfterParticleLandedEvent(EntityUid LandedParticle);
