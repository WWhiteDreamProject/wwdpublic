using System.Linq;
using Content.Server.Actions;
using Content.Server.NPC.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server._White.Headcrab;
using Content.Server.Zombies;
using Content.Shared.Zombies;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition.Components;
using Content.Shared._White.Headcrab;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._White.Headcrab;

public sealed partial class HeadcrabSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HeadcrabComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HeadcrabComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HeadcrabComponent, ThrowDoHitEvent>(OnHeadcrabDoHit);
        SubscribeLocalEvent<HeadcrabComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadcrabComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<HeadcrabComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<HeadcrabComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<HeadcrabComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<HeadcrabComponent, JumpActionEvent>(OnJump);
    }

    private void OnStartup(EntityUid uid, HeadcrabComponent component, ComponentStartup args)
    {
        _action.AddAction(uid, component.JumpAction);
    }

    private void OnHeadcrabDoHit(EntityUid uid, HeadcrabComponent component, ThrowDoHitEvent args)
    {
        if (component.IsDead)
            return;
        if (HasComp<ZombieComponent>(args.Target))
            return;
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;
        if (TryComp(args.Target, out MobStateComponent? mobState))
        {
            if (mobState.CurrentState is not MobState.Alive)
            {
                return;
            }
        }

        _inventory.TryGetSlotEntity(args.Target, "head", out var headItem);
        if (HasComp<IngestionBlockerComponent>(headItem))
            return;

        if (!_inventory.TryEquip(args.Target, uid, "mask", true))
            return;

        component.EquippedOn = args.Target;

        _popup.PopupEntity(Loc.GetString("headcrab-hit-entity-head"),
            args.Target, args.Target, PopupType.LargeCaution);

        _popup.PopupEntity(Loc.GetString("headcrab-hit-entity-head",
                ("entity", args.Target)),
            uid, uid, PopupType.LargeCaution);

        _popup.PopupEntity(Loc.GetString("headcrab-eat-other-entity-face",
            ("entity", args.Target)), args.Target, Filter.PvsExcept(uid), true, PopupType.Large);

        EnsureComp<PacifiedComponent>(uid);
        _stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(component.ParalyzeTime), true);
        _damageableSystem.TryChangeDamage(args.Target, component.Damage);
    }

    private void OnGotEquipped(EntityUid uid, HeadcrabComponent component, GotEquippedEvent args)
    {
        if (args.Slot != "mask")
            return;
        component.EquippedOn = args.Equipee;
        EnsureComp<PacifiedComponent>(uid);
        _npcFaction.AddFaction(uid, "Zombie");
    }

    private void OnUnequipAttempt(EntityUid uid, HeadcrabComponent component, BeingUnequippedAttemptEvent args)
    {
        if (args.Slot != "mask")
            return;
        if (component.EquippedOn != args.Unequipee)
            return;
        if (HasComp<ZombieComponent>(args.Unequipee))
            return;
        _popup.PopupEntity(Loc.GetString("headcrab-try-unequip"),
            args.Unequipee, args.Unequipee, PopupType.Large);
        args.Cancel();
    }

    private void OnGotEquippedHand(EntityUid uid, HeadcrabComponent component, GotEquippedHandEvent args)
    {
        if (HasComp<ZombieComponent>(args.User))
            return;
        if (component.IsDead)
            return;
        // _handsSystem.TryDrop(args.User, uid, checkActionBlocker: false);
        _damageableSystem.TryChangeDamage(args.User, component.Damage);
        _popup.PopupEntity(Loc.GetString("headcrab-entity-bite"),
            args.User, args.User);
    }

    private void OnGotUnequipped(EntityUid uid, HeadcrabComponent component, GotUnequippedEvent args)
    {
        if (args.Slot != "mask")
            return;
        component.EquippedOn = EntityUid.Invalid;
        RemCompDeferred<PacifiedComponent>(uid);
        var combatMode = EnsureComp<CombatModeComponent>(uid);
        _combat.SetInCombatMode(uid, true, combatMode);
        EnsureComp<NPCMeleeCombatComponent>(uid);
        _npcFaction.RemoveFaction(uid, "Zombie");
    }

    private void OnMeleeHit(EntityUid uid, HeadcrabComponent component, MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!HasComp<HumanoidAppearanceComponent>(entity))
                return;

            if (TryComp(entity, out MobStateComponent? mobState))
            {
                if (mobState.CurrentState is not MobState.Alive)
                {
                    return;
                }
            }

            _inventory.TryGetSlotEntity(entity, "head", out var headItem);
            if (HasComp<IngestionBlockerComponent>(headItem))
                return;

            var shouldEquip = _random.Next(1, 101) <= component.ChancePounce;
            if (!shouldEquip)
                return;

            var equipped = _inventory.TryEquip(entity, uid, "mask", true);
            if (!equipped)
                return;

            component.EquippedOn = entity;

            _popup.PopupEntity(Loc.GetString("headcrab-eat-entity-face"),
                entity, entity, PopupType.LargeCaution);

            _popup.PopupEntity(Loc.GetString("headcrab-hit-entity-head", ("entity", entity)),
                uid, uid, PopupType.LargeCaution);

            _popup.PopupEntity(Loc.GetString("headcrab-eat-other-entity-face",
                ("entity", entity)), entity, Filter.PvsExcept(entity), true, PopupType.Large);

            EnsureComp<PacifiedComponent>(uid);
            _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            _damageableSystem.TryChangeDamage(entity, component.Damage, origin: entity);
            break;
        }
    }

    private static void OnMobStateChanged(EntityUid uid, HeadcrabComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            component.IsDead = true;
        }
    }
    private void OnJump(EntityUid uid, HeadcrabComponent component, JumpActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var xform = Transform(uid);
        var mapCoords = _transform.ToMapCoordinates(args.Target);
        var direction = mapCoords.Position - _transform.GetMapCoordinates(xform).Position;

        _throwing.TryThrow(uid, direction, 7F, uid, 10F);
        if (component.HeadcrabJumpSound != null)
        {
            _audioSystem.PlayPvs(component.HeadcrabJumpSound, uid, component.HeadcrabJumpSound.Params);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<HeadcrabComponent>())
        {
            comp.Accumulator += frameTime;

            if (comp.Accumulator <= comp.DamageFrequency)
                continue;

            comp.Accumulator = 0;

            if (comp.EquippedOn is not { Valid: true } targetId)
                continue;
            if (HasComp<ZombieComponent>(comp.EquippedOn))
                return;
            if (TryComp(targetId, out MobStateComponent? mobState))
            {
                if (mobState.CurrentState is not MobState.Alive)
                {
                    _inventory.TryUnequip(targetId, "mask", true, true);
                    comp.EquippedOn = EntityUid.Invalid;
                    return;
                }
            }
            _damageableSystem.TryChangeDamage(targetId, comp.Damage);
            _popup.PopupEntity(Loc.GetString("headcrab-eat-entity-face"),
                targetId, targetId, PopupType.LargeCaution);
            _popup.PopupEntity(Loc.GetString("headcrab-eat-other-entity-face",
                ("entity", targetId)), targetId, Filter.PvsExcept(targetId), true);
        }
    }
}
