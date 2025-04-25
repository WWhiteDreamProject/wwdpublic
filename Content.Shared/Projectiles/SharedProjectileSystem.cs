using System.Linq;
using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._White.Penetrated;
using Content.Shared._White.Projectile;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Content.Shared.Standing;
using Robust.Shared.Network;

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem : EntitySystem
{
    public const string ProjectileFixture = "projectile";

    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly PenetratedSystem _penetrated = default!; // WD EDIT

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileHitEvent>(OnEmbedProjectileHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate, before: new[] { typeof(ActivatableUISystem), typeof(ItemToggleSystem), });
        SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EmbeddableProjectileComponent, PreventCollideEvent>(OnPreventCollision); // WD EDIT
    }

    // TODO: rename Embedded to Target in every context
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmbeddableProjectileComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AutoRemoveTime == null || comp.AutoRemoveTime > curTime)
                continue;

            if (comp.Target is { } targetUid)
                _popup.PopupClient(Loc.GetString("throwing-embed-falloff", ("item", uid)), targetUid, targetUid);

            RemoveEmbed(uid, comp);
        }
    }

    private void OnEmbedActivate(EntityUid uid, EmbeddableProjectileComponent component, ActivateInWorldEvent args)
    {
        // Nuh uh
        if (component.RemovalTime == null)
            return;

        if (args.Handled || !args.Complex || !TryComp<PhysicsComponent>(uid, out var physics) || physics.BodyType != BodyType.Static)
            return;

        args.Handled = true;

        if (component.Target is {} targetUid)
            _popup.PopupClient(Loc.GetString("throwing-embed-remove-alert-owner", ("item", uid), ("other", args.User)),
                args.User, targetUid);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.RemovalTime.Value,
            new RemoveEmbeddedProjectileEvent(), eventTarget: uid, target: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnEmbedRemove(EntityUid uid, EmbeddableProjectileComponent component, RemoveEmbeddedProjectileEvent args)
    {
        if (args.Cancelled)
            return;

        RemoveEmbed(uid, component, args.User);
    }

    public void RemoveEmbed(EntityUid uid, EmbeddableProjectileComponent component, EntityUid? remover = null)
    {
        component.AutoRemoveTime = null;
        component.Target = null;
        component.TargetBodyPart = null;

        var ev = new RemoveEmbedEvent(remover);
        RaiseLocalEvent(uid, ref ev);

        if (component.DeleteOnRemove)
        {
            QueueDel(uid);
            FreePenetrated(uid); // WD EDIT
            return;
        }

        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        var xform = Transform(uid);
        _physics.SetBodyType(uid, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(uid, xform);

        // Reset whether the projectile has damaged anything if it successfully was removed
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            projectile.Shooter = null;
            projectile.Weapon = null;
            projectile.DamagedEntity = false;
        }

        FreePenetrated(uid); // WD EDIT

        // Land it just coz uhhh yeah
        var landEv = new LandEvent(remover, true);
        RaiseLocalEvent(uid, ref landEv);
        _physics.WakeBody(uid, body: physics);

        // try place it in the user's hand
        if (remover is { } removerUid)
            _hands.TryPickupAnyHand(removerUid, uid);

        Dirty(uid, component);
    }

    private void OnEmbedThrowDoHit(EntityUid uid, EmbeddableProjectileComponent component, ThrowDoHitEvent args)
    {
        if (!component.EmbedOnThrow
            || HasComp<ThrownItemImmuneComponent>(args.Target)
            || _standing.IsDown(args.Target))
            return;

        TryEmbed(uid, args.Target, null, component, args.TargetPart);
    }

    private void OnEmbedProjectileHit(EntityUid uid, EmbeddableProjectileComponent component, ref ProjectileHitEvent args)
    {
        if (!(args.Target is { }) || _standing.IsDown(args.Target)
            || !TryComp(uid, out ProjectileComponent? projectile)
            || !TryEmbed(uid, args.Target, args.Shooter, component))
            return;

        // Raise a specific event for projectiles.
        var ev = new ProjectileEmbedEvent(projectile.Shooter, projectile.Weapon!.Value, args.Target);
        RaiseLocalEvent(uid, ref ev);
    }

    public bool TryEmbed(EntityUid uid, EntityUid target, EntityUid? user, EmbeddableProjectileComponent component, TargetBodyPart? targetPart = null, bool raiseEvent = true)
    {
        // WD EDIT START
        if (!TryComp<PhysicsComponent>(uid, out var physics)
            || physics.LinearVelocity.Length() < component.MinimumSpeed
            || _netManager.IsClient)
            return false;

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
            return true;
        // WD EDIT END

        _audio.PlayPredicted(component.Sound, uid, null);

        component.TargetBodyPart = targetPart;
        var ev = new EmbedEvent(user, target, targetPart);
        RaiseLocalEvent(uid, ref ev);

        if (component.AutoRemoveDuration != 0)
            component.AutoRemoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.AutoRemoveDuration);

        component.Target = target;

        Dirty(uid, component);
        return true;
    }

    private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
    {
        if (component.IgnoreShooter && (args.OtherEntity == component.Shooter || args.OtherEntity == component.Weapon))
            args.Cancelled = true;
    }

    public void SetShooter(EntityUid id, ProjectileComponent component, EntityUid shooterId)
    {
        if (component.Shooter == shooterId)
            return;

        component.Shooter = shooterId;
        Dirty(id, component);
    }

    private void OnExamined(EntityUid uid, EmbeddableProjectileComponent component, ExaminedEvent args)
    {
        if (!(component.Target is { } target))
            return;

        var targetIdentity = Identity.Entity(target, EntityManager);

        var loc = component.TargetBodyPart == null
            ? Loc.GetString("throwing-examine-embedded",
                ("embedded", uid),
                ("target", targetIdentity))
            : Loc.GetString("throwing-examine-embedded-part",
                ("embedded", uid),
                ("target", targetIdentity),
                ("targetName", Name(targetIdentity)), // WWDP
                ("targetPart", Loc.GetString($"body-part-{component.TargetBodyPart.ToString()}")));

        args.PushMarkup(loc);
    }

    // WD EDIT START
    private void OnPreventCollision(EntityUid uid, EmbeddableProjectileComponent component, ref PreventCollideEvent args)
    {
        // Opaque collision mask doesn't work for EmbeddableProjectileComponent
        if (component.PreventCollide && TryComp(args.OtherEntity, out FixturesComponent? fixtures) &&
            fixtures.Fixtures.All(fix => (fix.Value.CollisionLayer & (int) CollisionGroup.Opaque) == 0))
            args.Cancelled = true;
    }

    private void FreePenetrated(EntityUid uid, PenetratedProjectileComponent? penetratedProjectile = null)
    {
        if (!Resolve(uid, ref penetratedProjectile, false)
            || !penetratedProjectile.PenetratedUid.HasValue)
            return;

        _penetrated.FreePenetrated(penetratedProjectile.PenetratedUid.Value);
    }
    // WD EDIT END

    [Serializable, NetSerializable]
    private sealed partial class RemoveEmbeddedProjectileEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
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

/// <summary>
/// Raised after a projectile has dealt it's damage.
/// </summary>
[ByRefEvent]
public record struct AfterProjectileHitEvent(DamageSpecifier Damage, EntityUid Target);
