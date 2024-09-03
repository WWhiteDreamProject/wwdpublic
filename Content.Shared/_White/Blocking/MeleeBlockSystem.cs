using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._White.Blocking;

public sealed class MeleeBlockSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, MeleeBlockAttemptEvent>(OnBlockAttempt,
            after: new[] {typeof(BlockingSystem)});
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnHit,
            before: new[] {typeof(StaminaSystem), typeof(MeleeThrowOnHitSystem)});
        SubscribeLocalEvent<MeleeBlockComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<MeleeBlockComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("melee-block-component-delay", ("delay", ent.Comp.Delay.TotalSeconds)));
    }

    private void OnHit(Entity<MeleeWeaponComponent> ent, ref MeleeHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!ent.Comp.CanBeBlocked || !args.IsHit || args.Handled)
            return;

        if (args.Direction != null || args.HitEntities.Count != 1) // Heavy attacks are unblockable
            return;

        var hitEntity = args.HitEntities[0];

        if (hitEntity == args.User)
            return;

        var ev = new MeleeBlockAttemptEvent(args.User, args.BaseDamage + args.BonusDamage);
        RaiseLocalEvent(hitEntity, ev);

        if (ev.Handled)
            args.Handled = true;
    }

    private void OnBlockAttempt(Entity<HandsComponent> ent, ref MeleeBlockAttemptEvent args)
    {
        if (args.Handled || HasComp<BlockBlockerComponent>(ent))
            return;

        if (!TryComp(ent, out StatusEffectsComponent? statusEffects))
            return;

        var uid = ent.Comp.ActiveHandEntity;
        if (!TryComp(uid, out MeleeBlockComponent? block))
            return;

        if (TryComp(uid.Value, out ItemToggleComponent? toggle) && !toggle.Activated)
            return;

        _audio.PlayPredicted(block.BlockSound, ent, args.Attacker);
        _popupSystem.PopupPredicted(Loc.GetString("melee-block-event-blocked"), ent, args.Attacker);
        _damageable.TryChangeDamage(uid.Value, args.Damage);
        TryBlockBlocking(ent, block.Delay, true, statusEffects);
        args.Handled = true;
    }

    public bool TryBlockBlocking(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        return _statusEffect.TryAddStatusEffect<BlockBlockerComponent>(uid, "RecentlyBlocked", time, refresh);
    }
}
