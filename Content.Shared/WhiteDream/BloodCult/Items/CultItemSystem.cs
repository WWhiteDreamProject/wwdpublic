using Content.Shared.ActionBlocker;
using Content.Shared.Blocking;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Shared.Network;

namespace Content.Shared.WhiteDream.BloodCult.Items;

public sealed class CultItemSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CultItemComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CultItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultItemComponent, BeforeGettingThrownEvent>(OnBeforeGettingThrown);
        SubscribeLocalEvent<CultItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<CultItemComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<CultItemComponent, BeforeBlockingEvent>(OnBeforeBlocking);
        
        // Subscribe to ThrowAttemptEvent for all entities
        SubscribeLocalEvent<ThrowAttemptEvent>(OnGlobalThrowAttempt, before: new []{ typeof(ActionBlockerSystem) });
    }

    private void OnActivate(Entity<CultItemComponent> item, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
            
        if (CanUse(args.User, item))
            return;

        args.Handled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-generic"));
    }

    private void OnUseInHand(Entity<CultItemComponent> item, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;
            
        if (CanUse(args.User, item) ||
            // Allow non-cultists to remove embedded cultist weapons and getting knocked down afterwards on pickup
            (TryComp<EmbeddableProjectileComponent>(item.Owner, out var embeddable) && embeddable.Target != null))
            return;

        args.Handled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-generic"));
    }

    private void OnBeforeGettingThrown(Entity<CultItemComponent> item, ref BeforeGettingThrownEvent args)
    {
        if (CanUse(args.PlayerUid, item))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.PlayerUid, Loc.GetString("cult-item-component-throw-fail"), true);
    }

    private void OnEquipAttempt(Entity<CultItemComponent> item, ref BeingEquippedAttemptEvent args)
    {
        if (CanUse(args.EquipTarget, item))
            return;

        args.Cancel();
        KnockdownAndDropItem(item, args.Equipee, Loc.GetString("cult-item-component-equip-fail"));
    }

    private void OnMeleeAttempt(Entity<CultItemComponent> item, ref AttemptMeleeEvent args)
    {
        if (CanUse(args.PlayerUid, item))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.PlayerUid, Loc.GetString("cult-item-component-attack-fail"));
    }

    private void OnBeforeBlocking(Entity<CultItemComponent> item, ref BeforeBlockingEvent args)
    {
        if (CanUse(args.User, item))
            return;

        args.Cancel();
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-block-fail"));
    }

    // serverOnly is a very rough hack to make sure OnBeforeGettingThrown (that is only run server-side) can
    // show the popup while not causing several popups to show up with PopupEntity.
    private void KnockdownAndDropItem(Entity<CultItemComponent> item, EntityUid user, string message, bool serverOnly = false)
    {
        if (serverOnly)
            _popup.PopupEntity(message, user, user);
        else
            _popup.PopupEntity(message, user, user);
        
        if (!HasComp<ReturnableThrowingComponent>(item))
        {
            _stun.TryKnockdown(user, item.Comp.KnockdownDuration, true);
        }
        
        _hands.TryDrop(user);
    }

    private bool CanUse(EntityUid? uid, Entity<CultItemComponent> item) =>
        item.Comp.AllowUseToEveryone || HasComp<BloodCultistComponent>(uid) || HasComp<GhostComponent>(uid);
        
    /// <summary>
    /// Checks if a user can use a restricted cult item without CultItemComponent
    /// </summary>
    /// <returns>true if there are no restrictions (can be used)</returns>
    public bool CanUseRestrictedItem(EntityUid user, EntityUid itemUid)
    {
        // Check only specific items we know should be restricted
        var itemId = MetaData(itemUid).EntityPrototype?.ID;
        if (itemId != "CultBola" && itemId != "MirrorShieldCult" && itemId != "BloodSpear" && itemId != "UnholyHalberd")
            return true;
            
        // If the user is a cultist - allow use
        if (HasComp<BloodCultistComponent>(user) || HasComp<GhostComponent>(user))
            return true;
            
        // Non-cultists can't use the item - show message and stun
        var messageKey = itemId switch
        {
            "CultBola" => "cult-item-component-throw-bola-fail",
            "BloodSpear" => "cult-item-component-throw-spear-fail",
            "UnholyHalberd" => "cult-item-component-throw-halberd-fail",
            _ => "cult-item-component-throw-shield-fail"
        };
            
        _popup.PopupEntity(Loc.GetString(messageKey), user, user);
        
        // Stun the player when they try to use a cult item
        _stun.TryParalyze(user, TimeSpan.FromSeconds(3), true);
        
        return false;
    }

    /// <summary>
    /// Handles throw attempts for any item, including special cult items with or without CultItemComponent
    /// </summary>
    private void OnGlobalThrowAttempt(ThrowAttemptEvent ev)
    {
        // First, handle special restricted items without CultItemComponent
        if (!CanUseRestrictedItem(ev.Uid, ev.ItemUid))
        {
            ev.Cancel();
            return;
        }
        
        // Then handle items with CultItemComponent
        if (TryComp<CultItemComponent>(ev.ItemUid, out var cultItem))
        {
            if (!CanUse(ev.Uid, (ev.ItemUid, cultItem)))
                ev.Cancel();
        }
    }
}
