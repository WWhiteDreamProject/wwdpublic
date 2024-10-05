using System.Numerics;
using Content.Shared._White.Penetrated;
using Content.Shared._White.Projectile;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem : EntitySystem
{
    public const string ProjectileFixture = "projectile";

    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileHitEvent>(OnEmbedProjectileHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnEmbedThrowDoHit(EntityUid uid, EmbeddableProjectileComponent component, ThrowDoHitEvent args)
    {
        if (!component.EmbedOnThrow)
            return;

        Embed(uid, args.Target, null, component);
    }

    private void OnEmbedProjectileHit(EntityUid uid, EmbeddableProjectileComponent component, ref ProjectileHitEvent args)
    {
        Embed(uid, args.Target, args.Shooter, component);

        // Raise a specific event for projectiles.
        if (TryComp(uid, out ProjectileComponent? projectile))
        {
            var ev = new ProjectileEmbedEvent(projectile.Shooter!.Value, projectile.Weapon!.Value, args.Target);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    public void Embed(EntityUid uid, EntityUid target, EntityUid? user, EmbeddableProjectileComponent component, bool raiseEvent = true) // WD EDIT
    {
        // WD EDIT START
        if (!TryComp<PhysicsComponent>(uid, out var physics)
            || physics.LinearVelocity.Length() < component.MinimumSpeed
            || _netManager.IsClient)
            return;

        var attemptEmbedEvent = new AttemptEmbedEvent(user, target);
        RaiseLocalEvent(uid, ref attemptEmbedEvent);

        var xform = Transform(uid);

        if (!TryComp<PenetratedProjectileComponent>(uid, out var penetratedProjectile)
            || !penetratedProjectile.PenetratedUid.HasValue
            || (penetratedProjectile.PenetratedUid != target
                && !HasComp<PenetratedComponent>(target)))
        {
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
            _physics.SetBodyType(uid, BodyType.Static, body: physics);
            _transform.SetParent(uid, xform, target);
        }
        // WD EDIT END

        if (component.Offset != Vector2.Zero)
        {
            _transform.SetLocalPosition(uid, xform.LocalPosition + xform.LocalRotation.RotateVec(component.Offset),
                xform);
        }

        // WD EDIT START
        if (!raiseEvent)
            return;
        // WD EDIT END

        _audio.PlayPredicted(component.Sound, uid, null);
        var ev = new EmbedEvent(user, target);
        RaiseLocalEvent(uid, ref ev);
    }

    private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
    {
        if (component.IgnoreShooter && (args.OtherEntity == component.Shooter || args.OtherEntity == component.Weapon))
        {
            args.Cancelled = true;
        }
    }

    public void SetShooter(EntityUid id, ProjectileComponent component, EntityUid shooterId)
    {
        if (component.Shooter == shooterId)
            return;

        component.Shooter = shooterId;
        Dirty(id, component);
    }

    [Serializable, NetSerializable]
    private sealed partial class RemoveEmbeddedProjectileEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

    /// <summary>
    /// Prevent players with the Pacified status effect from throwing embeddable projectiles.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<EmbeddableProjectileComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw-embed");
    }
}

[Serializable, NetSerializable]
public sealed class ImpactEffectEvent : EntityEventArgs
{
    public string Prototype;
    public NetCoordinates Coordinates;

    public ImpactEffectEvent(string prototype, NetCoordinates coordinates)
    {
        Prototype = prototype;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Raised when an entity is just about to be hit with a projectile but can reflect it
/// </summary>
[ByRefEvent]
public record struct ProjectileReflectAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled);

/// <summary>
/// Raised when a projectile hits an entity
/// </summary>
[ByRefEvent]
public record struct ProjectileHitEvent(DamageSpecifier Damage, EntityUid Target, EntityUid? Shooter = null);

// WD EDIT START
[Serializable, NetSerializable]
public sealed partial class RemoveEmbeddedProjectileEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
// WD EDIT END
