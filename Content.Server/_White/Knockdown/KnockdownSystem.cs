using Content.Server.Jittering;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable.Events;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._White.Knockdown;

public sealed class KnockdownSystem : EntitySystem
{
    [Dependency] private readonly StunSystem _sharedStun = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly StutteringSystem _stutter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockdownOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<KnockdownOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<KnockdownOnCollideComponent, ThrowDoHitEvent>(OnThrowDoHit);
    }

    private void OnMeleeHit(EntityUid uid, KnockdownOnHitComponent component, MeleeHitEvent args)
    {
        var ev = new KnockdownOnHitAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        foreach (var hitEntity in args.HitEntities)
            Knockdown(hitEntity, component);
    }

    private void OnProjectileHit(EntityUid uid, KnockdownOnCollideComponent component, ProjectileHitEvent args) => Knockdown(args.Target, component);

    private void OnThrowDoHit(EntityUid uid, KnockdownOnCollideComponent component, ThrowDoHitEvent args) => Knockdown(args.Target, component);

    private void Knockdown(EntityUid target, BaseKnockdownOnComponent component)
    {
        if (!TryComp<StatusEffectsComponent>(target, out var statusEffects))
            return;

        if (component.JitterTime > TimeSpan.Zero)
            _jitter.DoJitter(target, component.JitterTime, true, status: statusEffects);

        if (component.StutterTime > TimeSpan.Zero)
            _stutter.DoStutter(target, component.StutterTime, true, statusEffects);

        if (component.Delay == TimeSpan.Zero)
        {
            _sharedStun.TryKnockdown(target, component.KnockdownTime, true, statusEffects);
            return;
        }

        var knockdown = EnsureComp<KnockComponent>(target);
        knockdown.Delay = _timing.CurTime + component.Delay;
        knockdown.KnockdownTime = component.KnockdownTime;
        knockdown.DropHeldItemsBehavior = component.DropHeldItemsBehavior;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<KnockComponent>();
        while (query.MoveNext(out var uid, out var delayedKnockdown))
        {
            if (delayedKnockdown.Delay > _timing.CurTime)
                continue;

            _sharedStun.TryKnockdown(uid, delayedKnockdown.KnockdownTime, true, delayedKnockdown.DropHeldItemsBehavior);
            RemCompDeferred<KnockComponent>(uid);
        }
    }
}
