using System.Linq;
using System.Numerics;
using Content.Shared._White.Wizard.Projectiles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._White.Wizard.Trail;

public sealed class TrailSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    [Dependency] private readonly TransformSystem _transform = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new TrailOverlay(EntityManager, _protoMan, _timing));

        SubscribeLocalEvent<TrailComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TrailComponent, ComponentRemove>(OnRemove);

        _xformQuery = GetEntityQuery<TransformComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        UpdatesOutsidePrediction = true;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<TrailOverlay>();
    }
    private void OnStartup(Entity<TrailComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Accumulator = ent.Comp.Frequency;
        ent.Comp.LerpAccumulator = ent.Comp.LerpTime;
    }

    private void OnRemove(Entity<TrailComponent> ent, ref ComponentRemove args)
    {
        var (_, comp) = ent;

        if (!comp.SpawnRemainingTrail
            || comp.TrailData.Count == 0
            || comp.Frequency <= 0f
            || comp.Lifetime <= 0f
            || comp.LastCoords.MapId != _eye.CurrentEye.Position.MapId
            || comp.RenderedEntity != null && TerminatingOrDeleted(comp.RenderedEntity.Value))
            return;

        var remainingTrail = Spawn(null, comp.LastCoords);
        EnsureComp<TimedDespawnComponent>(remainingTrail).Lifetime = comp.Lifetime;

        var trail = CopyComp(ent, remainingTrail, comp);
        trail.SpawnRemainingTrail = false;
        trail.Frequency = 0f;
        trail.TrailData = comp.TrailData;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<TrailComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trail, out var xform))
        {
            if (trail.Lifetime <= 0f)
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform, _xformQuery);
            trail.LastCoords = new MapCoordinates(position, xform.MapID);

            Lerp(trail, position, frameTime);

            trail.Accumulator += frameTime;

            // Assuming that lifetime and frequency don't change
            if (trail.Accumulator > trail.Lifetime && trail.Lifetime < trail.Frequency && trail.TrailData.Count > 0)
            {
                trail.ParticleCount = 0;
                trail.TrailData.Clear();
            }

            if (trail.Accumulator <= trail.Frequency)
                continue;

            trail.Accumulator = 0f;

            if (trail.SpawnEntityPosition != null && !Exists(trail.SpawnEntityPosition))
                continue;

            Angle angle;
            if (_physicsQuery.TryComp(uid, out var physics) && physics.LinearVelocity.LengthSquared() > 0)
                angle = physics.LinearVelocity.ToAngle();
            else
                angle = xform.LocalRotation;

            var startAngle = trail.StartAngle + angle;
            var endAngle = trail.EndAngle + angle;

            SpawnParticle(trail, position, rotation, new Angle((endAngle.Theta + startAngle.Theta) * 0.5).ToVec(), xform.MapID);
            for (var i = 0; i < trail.ParticleAmount - 1; i++)
            {
                if (trail.MaxParticleAmount > 0 && trail.ParticleCount >= trail.MaxParticleAmount)
                    break;
                var direction = new Angle(startAngle + (endAngle - startAngle) * i / (trail.ParticleAmount - 1)).ToVec();
                SpawnParticle(trail, position, rotation, direction, xform.MapID);
            }
        }
    }

    private void SpawnParticle(TrailComponent trail, Vector2 position, Angle rotation, Vector2 direction, MapId mapId)
    {
        if (Exists(trail.SpawnEntityPosition))
        {
            position = _transform.GetWorldPosition(trail.SpawnEntityPosition.Value, _xformQuery);
            if (trail.SpawnPosition != null)
                position += trail.SpawnPosition.Value;
        }
        else if (trail.SpawnPosition != null)
            position = trail.SpawnPosition.Value;

        var targetPos = position + direction * trail.Radius;
        if (trail.TrailData.Count < MathF.Max(trail.ParticleAmount, trail.ParticleAmount * trail.Lifetime / trail.Frequency))
        {
            trail.ParticleCount++;
            trail.TrailData.Add(
                new TrailData(
                    targetPos,
                    trail.Velocity,
                    mapId,
                    direction,
                    rotation,
                    trail.Color,
                    trail.Scale,
                    _timing.CurTime));
        }
        else if (trail.TrailData.Count > 0)
        {
            if (trail.CurIndex >= trail.TrailData.Count || trail.Sprite == null)
                trail.CurIndex = 0;

            var data = trail.TrailData[trail.CurIndex];

            data.Color = trail.Color;
            data.Position = targetPos;
            data.Velocity = trail.Velocity;
            data.MapId = mapId;
            data.Direction = direction;
            data.Angle = rotation;
            data.Scale = trail.Scale;
            data.SpawnTime = _timing.CurTime;

            if (trail.Sprite == null)
            {
                trail.TrailData.RemoveAt(0);
                trail.TrailData.Add(data);
            }
            else
                trail.CurIndex++;
        }
    }

    private void Lerp(TrailComponent trail, Vector2 position, float frameTime)
    {
        trail.LerpAccumulator += frameTime;

        if (trail.LerpAccumulator <= trail.LerpTime)
            return;

        trail.LerpAccumulator = 0;

        foreach (var data in trail.TrailData)
        {
            if (trail.LerpDelay > _timing.CurTime - data.SpawnTime)
                return;

            if (trail.AlphaLerpAmount > 0f)
            {
                var alphaTarget = trail.AlphaLerpTarget is >= 0f and <= 1f ? trail.AlphaLerpTarget : 0f;
                data.Color.A = float.Lerp(data.Color.A, alphaTarget, trail.AlphaLerpAmount);
            }

            if (trail.ScaleLerpAmount > 0f)
            {
                var scaleTarget = trail.ScaleLerpTarget >= 0f ? trail.ScaleLerpTarget : 0f;
                data.Scale = float.Lerp(data.Scale, scaleTarget, trail.ScaleLerpAmount);
            }

            data.Position += data.Direction * data.Velocity;

            if (trail.PositionLerpAmount > 0f)
                data.Position = Vector2.Lerp(data.Position, position, trail.PositionLerpAmount);

            if (trail.VelocityLerpAmount > 0f)
                data.Velocity = float.Lerp(data.Velocity, trail.VelocityLerpTarget, trail.VelocityLerpAmount);
        }

        foreach (var lerpData in trail.AdditionalLerpData.Where(x => x.LerpAmount > 0f))
        {
            lerpData.Value = float.Lerp(lerpData.Value, lerpData.LerpTarget, lerpData.LerpAmount);

            if (lerpData.Property != null)
                AnimationHelper.SetAnimatableProperty(trail, lerpData.Property!, lerpData.Value); // WD edit - make lerpData.Property nullable
        }
    }
}
