using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._White.Event;
using Robust.Shared.Audio.Systems;
using Content.Server.Store.Systems;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Content.Shared.Ghost;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server._White.Event;
public class EventItemDispenserSystem : SharedEventItemDispenserSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    [Dependency] private readonly IAdminManager _admeme = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EventItemDispenserComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<EventItemDispenserComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EventItemDispenserComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<EventItemDispenserComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<EventItemDispenserComponent, EventItemDispenserNewConfigBoundUserInterfaceMessage>(OnMessage);

        SubscribeLocalEvent<EventDispensedComponent, ComponentStartup>(OnDispensedStartup);
        SubscribeLocalEvent<EventDispensedComponent, ComponentRemove>(OnDispensedRemove);
    }



    private void OnMessage(EntityUid uid, EventItemDispenserComponent comp, EventItemDispenserNewConfigBoundUserInterfaceMessage msg)
    {
        var adminData = _admeme.GetAdminData(msg.Actor);
        if (adminData == null || !adminData.CanAdminPlace())
        {
            return;
        }
        string newProto = ValidateProto(msg.DispensingPrototype, comp.DispensingPrototype);
        if (comp.DispensingPrototype != newProto) {
            comp.DispensingPrototype = newProto;
            DeleteAll(uid, comp);
        }
        comp.AutoDispose = msg.AutoDispose;
        comp.CanManuallyDispose = msg.CanManuallyDispose;
        comp.Infinite = msg.Infinite;
        comp.Limit = msg.Limit;
        comp.DisposedReplacement = ValidateProto(msg.DisposedReplacement, comp.DisposedReplacement);
        comp.ReplaceDisposedItems = msg.ReplaceDisposedItems;
        comp.AutoCleanUp = msg.AutoCleanUp;
        Dirty(uid, comp);
    }

    private void OnDispensedStartup(EntityUid uid, EventDispensedComponent comp, ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var contManager))
            return;

        RecursiveSlaveAllContents(uid, comp);
    }

    private void RecursiveSlaveAllContents(EntityUid item, EventDispensedComponent comp, int depth = 0)
    {
        if (depth >= 5) // what the fuck kind of item is that?
            throw new ArgumentException($"Item Uid:{item}, proto:\"{MetaData(item).EntityPrototype?.ID}\" has FIVE levels of storage component entities stored in each other. What the fuck?");
        if (!HasComp<ContainerManagerComponent>(item))
            return;
        depth++;
        RaiseLocalEvent(item, new ForceSpawnAmmoEvent());
        var containers = _container.GetAllContainers(item);
        bool more = false;
        List<EntityUid> ents = new();
        foreach (var container in containers)
        {
            ents.AddRange(container.ContainedEntities);
        }
        comp.Slaved.AddRange(ents);
        foreach (var ent in ents)
        {
            RecursiveSlaveAllContents(ent, comp, depth);
        }
    }

    private void OnDispensedRemove(EntityUid uid, EventDispensedComponent comp, ComponentRemove args)
    {
        foreach(var item in comp.Slaved)
        {
            QueueDel(item);
        }
    }

    private void OnRemove(EntityUid uid, EventItemDispenserComponent comp, ComponentRemove args)
    {
        if (comp.AutoCleanUp)
        {
            DeleteAll(uid, comp);
        }
        else
        {
            ReleaseAll(uid, comp);
        }
    }

    private void OnExamine(EntityUid uid, EventItemDispenserComponent comp, ExaminedEvent args)
    {
        string desc = "";
        if (!comp.Infinite)
            desc = $"event-item-dispenser-examine-finite{(comp.CanManuallyDispose ? "-manualdispose" : "")}{(comp.Limit == 1 ? "-single" : "")}";
        else
        {
            desc = $"event-item-dispenser-examine-infinite{(comp.AutoDispose ? "-autodispose" : "")}{(comp.CanManuallyDispose ? "-manualdispose" : "")}{(comp.Limit == 1 ? "-single" : "")}";
        }
        int remaining = GetRemaining(args.Examiner, comp);

        args.PushMarkup(
            Loc.GetString(
                "event-item-dispenser-item-name",
                ("itemName", _loc.GetEntityData(comp.DispensingPrototype).Name)), 1);
        args.PushMarkup(
            Loc.GetString(
                desc,
                ("remaining", remaining),
                ("limit", comp.Limit),
                ("noLimit", comp.Limit <= 0) // this is getting ridiculous
                ), 0);
    }

    private void OnInteractUsing(EntityUid uid, EventItemDispenserComponent comp, InteractUsingEvent args)
    {
        EntityUid user = args.User;
        EntityUid item = args.Used;

        if (CanOpenUI(user))
        {
            var uicomp = Comp<UserInterfaceComponent>(uid);
            string newProto = MetaData(item).EntityPrototype?.ID ?? comp.DispensingPrototype;
            if(_ui.TryOpenUi((uid, uicomp), EventItemDispenserUiKey.Key, user))
                _ui.ServerSendUiMessage((uid, uicomp), EventItemDispenserUiKey.Key, new EventItemDispenserNewProtoBoundUserInterfaceMessage(newProto), args.User);
            return;
        }
        if( comp.CanManuallyDispose &&
            TryComp<EventDispensedComponent>(item, out var dispensed) &&
            dispensed.ItemOwner == user &&
            dispensed.Dispenser == comp.Owner)
        {
            Recycle(item, comp, false);
            PopupRemaining(user, comp);
            _audio.PlayPvs(comp.ManualDisposeSound, uid);
        }
    }

    private void OnActivateInWorld(EntityUid uid, EventItemDispenserComponent comp, ActivateInWorldEvent args)
    {
        if(Deleted(_hands.GetActiveItem(args.User)))
            DispenseRequest(uid, args.User, comp);
    }

    private void DispenseRequest(EntityUid uid, EntityUid user, EventItemDispenserComponent comp)
    {
        if (CanOpenUI(user))
        {
            var uicomp = Comp<UserInterfaceComponent>(uid);
            _ui.TryToggleUi((uid, uicomp), EventItemDispenserUiKey.Key, user);
            return;
        }
        PruneItemList(user, comp);
        var items = comp.dispensedItems[user];
        comp.dispensedItemsAmount.TryGetValue(user, out int allTimeAmount);

        if (comp.Limit > 0)
        {
            if (!comp.Infinite && allTimeAmount >= comp.Limit) // no fancy bluespace disposal shit if dispenser is meant to be finite.
            {
                _audio.PlayPvs(comp.FailSound, uid);
                _popup.PopupEntity(_loc.GetString("event-item-dispenser-out-of-stock"), uid, user);
                return;
            }

            if (items.Count >= comp.Limit)
            {
                if (comp.AutoDispose)
                    DeleteOne(user, comp);
                else
                {
                    _audio.PlayPvs(comp.FailSound, uid);
                    _popup.PopupEntity(_loc.GetString("event-item-dispenser-limit-reached"), uid, user);
                    return;
                }
            }
        }
        Dispense(user, comp);
    }

    /// <summary>
    /// Used to delete an oldest item when trying to dispense over limit.
    /// Does not update the dispensedItems dict, so calling it multiple times will not work.
    /// </summary>
    private void DeleteOne(EntityUid owner, EventItemDispenserComponent comp)
    {
        var items = comp.dispensedItems[owner];
        if (items.Count > 0)
            Recycle(items.First(), comp);
    }

    private void DeleteAll(EntityUid uid, EventItemDispenserComponent comp)
    {
        foreach (var items in comp.dispensedItems.Values)
        {
            foreach (var item in items)
            {
                if (!TerminatingOrDeleted(item)) // do i have to?
                {
                    Recycle(item, comp, false); // no fancy effects
                } // "{{{{}}}}" is non-negotiable
            }
        }
    }

    private void ReleaseAll(EntityUid dispenser, EventItemDispenserComponent comp) 
    {
        foreach (var items in comp.dispensedItems.Values)
        {
            foreach (var item in items)
            {
                if (!TerminatingOrDeleted(item))
                {
                    // not clearing dicts because this is only called immediately prior dispenser comp deletion
                    RemComp<EventDispensedComponent>(item);
                } // "{{{{}}}}" is non-negotiable
            }
        }
    }

    private void Recycle(EntityUid item, EventItemDispenserComponent comp, bool replace = true)
    {
        if(TryComp<EventDispensedComponent>(item, out var itemcomp))
        {
            DebugTools.Assert(comp.Owner == itemcomp.Dispenser, "Attempted to recycle dispensed item in wrong dispenser.");
            comp.dispensedItemsAmount[itemcomp.ItemOwner]--;
            //comp.dispensedItems[itemcomp.ItemOwner].Remove(item); // fucks up foreach loops
            DebugTools.Assert(comp.dispensedItemsAmount[itemcomp.ItemOwner] >= 0, "EventItemDispenser ended up with negative total items dispensed.");
            if (comp.ReplaceDisposedItems && replace)
            {
                var mapPos = _transform.ToMapCoordinates(new Robust.Shared.Map.EntityCoordinates(item, default));
                Spawn(comp.DisposedReplacement, mapPos);
            }

            Del(item);
        }
    }

    /// <summary>
    /// This hot mess does a lot of things at once:
    ///     * Spawn and configure the item
    ///       * Raise ItemPurchasedEvent on the item just in case
    ///       * Add and configure EventDispensedComponent, used for easier manual disposals (see <see cref="OnInteractUsing(EntityUid, EventItemDispenserComponent, InteractUsingEvent)"/>)
    ///     * Keep track on how many items there are
    ///     * Put said item into user's hands
    ///     * Play a sound
    ///
    /// In short: this is stupid and ugly. TODO fix this. // oh you sweet summer child
    /// </summary>
    /// <param name="user"></param>
    /// <param name="comp"></param>
    private void Dispense(EntityUid user, EventItemDispenserComponent comp)
    {
        var mapPos = _transform.ToMapCoordinates(new Robust.Shared.Map.EntityCoordinates(user, default));
        var item = Spawn(comp.DispensingPrototype, mapPos);
        var ev = new ItemPurchasedEvent(user);
        RaiseLocalEvent(item, ref ev); // erectin' a vendomat
        var dispensedComp = AddComp<EventDispensedComponent>(item);
        dispensedComp.Dispenser = comp.Owner;
        dispensedComp.ItemOwner = user;

        comp.dispensedItems[user].Add(item);

        comp.dispensedItemsAmount.TryGetValue(user, out int amount);
        comp.dispensedItemsAmount[user] = amount + 1;

        _hands.TryPickup(user, item);
        PopupRemaining(user, comp);
        _audio.PlayPvs(comp.DispenseSound, comp.Owner);
    }

    private void PopupRemaining(EntityUid user, EventItemDispenserComponent comp)
    {
        int remaining = GetRemaining(user, comp);
        if (comp.Infinite)
            remaining = comp.Limit - remaining; // to instead indicate how much items we have already taken
        _popup.PopupEntity($"{remaining}/{comp.Limit}", comp.Owner, user);
    }

    private string ValidateProto(string proto, string backup)
    {
        return _proto.HasIndex<EntityPrototype>(proto) ? proto : backup;
    }

    private bool CanOpenUI(EntityUid user)
    {
        var adminData = _admeme.GetAdminData(user);
        return adminData != null && adminData.CanAdminPlace() &&
            HasComp<GhostComponent>(user);
    }

    /// <summary>
    /// Self-explanatory.
    /// </summary>
    private int GetRemaining(EntityUid user, EventItemDispenserComponent comp)
    {
        if (comp.Limit <= 0)
            return 9001;
        if (comp.Infinite)
        {
            PruneItemList(user, comp);
            return comp.dispensedItems.ContainsKey(user) ? comp.Limit - comp.dispensedItems[user].Count : comp.Limit;
        }
        else
            return comp.dispensedItemsAmount.ContainsKey(user) ? comp.Limit - comp.dispensedItemsAmount[user] : comp.Limit;

    }
    private void PruneItemList(EntityUid user, EventItemDispenserComponent comp)
    {
        comp.dispensedItems[user] = comp.dispensedItems.GetOrNew(user).Where(item => !TerminatingOrDeleted(item)).ToList();
    }
}

