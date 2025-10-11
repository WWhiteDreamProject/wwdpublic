using Content.Shared.Blocking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._White.BloodCult.CultItem;

public sealed class CultItemSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultItemComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CultItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultItemComponent, BeforeGettingThrownEvent>(OnBeforeGettingThrown);
        SubscribeLocalEvent<CultItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<CultItemComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<CultItemComponent, BeforeBlockingEvent>(OnBeforeBlocking);
    }

    private void OnActivate(Entity<CultItemComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, args.User))
            return;

        args.Handled = true;
        KnockdownAndDropItem(ent, args.User, Loc.GetString("cult-item-component-generic"));
    }

    private void OnUseInHand(Entity<CultItemComponent> ent, ref UseInHandEvent args)
    {
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, args.User) ||
            // Allow non-cultists to remove embedded cultist weapons and getting knocked down afterwards on pickup
            TryComp<EmbeddableProjectileComponent>(ent.Owner, out var embeddable) && embeddable.EmbeddedIntoUid != null)
            return;

        args.Handled = true;
        KnockdownAndDropItem(ent, args.User, Loc.GetString("cult-item-component-generic"));
    }

    private void OnBeforeGettingThrown(Entity<CultItemComponent> ent, ref BeforeGettingThrownEvent args)
    {
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, args.PlayerUid))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(ent, args.PlayerUid, Loc.GetString("cult-item-component-throw-fail"), false);
    }

    private void OnEquipAttempt(Entity<CultItemComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, args.Equipee))
            return;

        args.Cancel();
        KnockdownAndDropItem(ent, args.Equipee, Loc.GetString("cult-item-component-equip-fail"));
    }

    private void OnMeleeAttempt(Entity<CultItemComponent> ent, ref AttemptMeleeEvent args)
    {
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, args.User))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(ent, args.User, Loc.GetString("cult-item-component-attack-fail"));
    }

    private void OnBeforeBlocking(Entity<CultItemComponent> ent, ref BeforeBlockingEvent args)
    {
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, args.User))
            return;

        args.Cancel();
        KnockdownAndDropItem(ent, args.User, Loc.GetString("cult-item-component-block-fail"));
    }

    // predict is a very rough hack to make sure OnBeforeGettingThrown (that is only run server-side) can
    // show the popup while not causing several popups to show up with PopupEntity.
    private void KnockdownAndDropItem(Entity<CultItemComponent> ent, EntityUid user, string message, bool predict = true)
    {
        if (predict)
            _popup.PopupPredicted(message, ent, user);
        else
            _popup.PopupEntity(message, ent, user);
        _stun.TryKnockdown(user, ent.Comp.KnockdownDuration, true);
        _hands.TryDrop(user, ent);
    }
}
