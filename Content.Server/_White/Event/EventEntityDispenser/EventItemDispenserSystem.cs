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
using System.Diagnostics;

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

    //[Dependency] private readonly ILogManager _log = default!;
    //ISawmill _sawmill = default!;


    public override void Initialize()
    {
        //_sawmill = _log.GetSawmill("EventDispenser");
        SubscribeLocalEvent<EventItemDispenserComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<EventItemDispenserComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EventItemDispenserComponent, InteractHandEvent>(OnInteractHand);

        SubscribeLocalEvent<EventItemDispenserComponent, EventItemDispenserNewConfigBoundUserInterfaceMessage>(OnMessage);

        SubscribeLocalEvent<EventDispensedComponent, ComponentStartup>(OnDispensedStartup);
        SubscribeLocalEvent<EventDispensedComponent, ComponentRemove>(OnDispensedRemove);


        // todo add ComponentStart handler to EventDispensedComponent to properly dispose of items spawning with filled storage
        //      * add a List<EntityUid> var to EventDispensedComponent tracking all slaved items
        //      * handle dropping stuff from storage that is not slaved, in case it's not handled by default (i think it's not)

        // todo examine for per-player limits?
        //      * maybe adding a List<EntityUid> variable to EventItemDispenserComponent for strictly client-side usage?
        //        >will require server to confirm the item count (otherwise items outside pvs will desync the counter one way or another)
        //      * keep examine server-sided
        //        >can examine tooltip be "postponed" until receiving relevant info?
    }

    private string ValidateProto(string proto, string backup)
    {
        return _proto.HasIndex(proto) ? proto : backup;
    }
    private bool CanOpenUI(EntityUid user)
    {
        var adminData = _admeme.GetAdminData(user);
        return adminData != null && adminData.CanAdminPlace() &&
            MetaData(user).EntityPrototype?.ID == GameTicker.AdminObserverPrototypeName;
    }

    private void OnMessage(EntityUid uid, EventItemDispenserComponent comp, EventItemDispenserNewConfigBoundUserInterfaceMessage msg)
    {
        var adminData = _admeme.GetAdminData(msg.Actor);
        if (adminData == null || !adminData.CanAdminPlace())
        {
            //_sawmill.Warning($"")
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
        //if(msg.Limit < comp.Limit) // too hard to include all possible cases without a reset every time limit is decreased.
        //{
        //    DeleteExcess(comp);
        //}
        comp.Limit = msg.Limit; // It still works ok.
        
        //if(string.IsNullOrWhiteSpace(msg.DisposedReplacement)) // if the prototype is set to null, just set the relevant flag to false
        //{
        //    comp.ReplaceDisposedItems = false;
        //}
        //else
        //{
        comp.DisposedReplacement = ValidateProto(msg.DisposedReplacement, comp.DisposedReplacement);
        comp.ReplaceDisposedItems = msg.ReplaceDisposedItems;
        //}
        comp.AutoCleanUp = msg.AutoCleanUp;
        Dirty(uid, comp);
    }
    private void OnDispensedStartup(EntityUid uid, EventDispensedComponent comp, ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var contManager))
            return;
        var containers = _container.GetAllContainers(uid);
        foreach(var container in containers)
        {
            comp.Slaved.AddRange(container.ContainedEntities);
        }
    }
    private void OnDispensedRemove(EntityUid uid, EventDispensedComponent comp, ComponentRemove args)
    {

        foreach(var item in comp.Slaved)
        {
            // will require either caching parent dispenser's ReplaceDisposedItems and DisposedReplacement, or looking up EventItemDispenserComponent.
            // the latter will break if dispenser is being removed.
            //if (comp.ReplaceDisposedItems)
            //{
            //    var mapPos = _transform.ToMapCoordinates(new Robust.Shared.Map.EntityCoordinates(item, default));
            //    Spawn(comp.DisposedReplacement, mapPos);
            //}
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

    private void OnInteractHand(EntityUid uid, EventItemDispenserComponent comp, ref InteractHandEvent args)
    {
        EntityUid user = args.User;

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

    /// <summary>
    /// Deletes items over limit. Only called when <see cref="EventItemDispenserComponent.Limit"/> is reduced.
    /// </summary>
    private void DeleteExcess(EventItemDispenserComponent comp)
    {
        foreach (var owner in comp.dispensedItems.Keys)
        {
            var items = comp.dispensedItems[owner];
            if (items.Count <= comp.Limit)
                continue;

            items = items.AsEnumerable().Reverse().ToList();
            while (items.Count > comp.Limit)
            {
                var item = items.Pop();
                Recycle(item, comp);
            }
        }
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
            Debug.Assert(comp.Owner == itemcomp.Dispenser, "Attempted to recycle dispensed item in wrong dispenser.");
            comp.dispensedItemsAmount[itemcomp.ItemOwner]--;
            comp.dispensedItems[itemcomp.ItemOwner].Remove(item);
            Debug.Assert(comp.dispensedItemsAmount[itemcomp.ItemOwner] >= 0, "EventItemDispenser ended up with negative total items dispensed.");
            if (comp.ReplaceDisposedItems && replace)
            {
                var mapPos = _transform.ToMapCoordinates(new Robust.Shared.Map.EntityCoordinates(item, default));
                Spawn(comp.DisposedReplacement, mapPos);
            }

            QueueDel(item);
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
    /// In short: this is stupid and ugly. TODO fix this.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="comp"></param>
    private void Dispense(EntityUid user, EventItemDispenserComponent comp)
    {
        var item = Spawn(comp.DispensingPrototype, new Robust.Shared.Map.EntityCoordinates(user, default)); // am i retarded?
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
        _popup.PopupEntity($"{GetRemaining(user, comp)}/{comp.Limit}", comp.Owner, user);
    }

    private int GetRemaining(EntityUid user, EventItemDispenserComponent comp)
    {
        if (comp.Infinite)
            return comp.dispensedItems[user].Count;
        else
            return comp.Limit - comp.dispensedItemsAmount[user];
    }


    private void PruneItemList(EntityUid user, EventItemDispenserComponent comp)
    {
        comp.dispensedItems[user] = comp.dispensedItems.GetOrNew(user).Where(item => !TerminatingOrDeleted(item)).ToList();
    }
}

