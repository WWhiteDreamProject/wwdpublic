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

namespace Content.Server._White.Event;
public class EventItemDispenserSystem : SharedEventItemDispenserSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;



    public override void Initialize()
    {
        SubscribeLocalEvent<EventItemDispenserComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<EventItemDispenserComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EventItemDispenserComponent, InteractHandEvent>(OnInteractHand);

        // todo add ComponentStart handler to EventDispensedComponent to properly dispose of items spawning with filled storage
        //      * add a List<EntityUid> var to EventDispensedComponent tracking all slaved items
        //      * handle dropping stuff from storage that is not slaved, in case it's not handled by default (i think it's not)

        // todo examine for per-player limits?
        //      * maybe adding a List<EntityUid> variable to EventItemDispenserComponent for strictly client-side usage?
        //        >will require server to confirm the item count (otherwise items outside pvs will desync the counter one way or another)
        //      * keep examine server-sided
        //        >can examine tooltip be "postponed" until receiving relevant info?


    }
    private void OnRemove(EntityUid uid, EventItemDispenserComponent comp, ComponentRemove args)
    {
        foreach(var items in comp.dispensedItems.Values)
        {
            foreach (var item in items)
            {
                if (!TerminatingOrDeleted(item)) // do i have to?
                {
                    QueueDel(item); // no fancy effects
                }
            }
        }
    }
    private void OnInteractUsing(EntityUid uid, EventItemDispenserComponent comp, InteractUsingEvent args)
    {
        if(MetaData(args.User).EntityPrototype?.ID == GameTicker.AdminObserverPrototypeName)
        {
            _popup.PopupEntity("TODO aghost configuring UI", uid, args.User); // todo ditto
            return;
        }
        if( comp.CanManuallyDispose &&
            TryComp<EventDispensedComponent>(args.Used, out var dispensed) &&
            dispensed.ItemOwner == args.User &&
            dispensed.Dispenser == comp.Owner)
        {
            QueueDel(args.Used);
            _audio.PlayPvs(comp.ManualDisposeSound, uid);
            comp.dispensedItemsAmount[dispensed.ItemOwner] -= 1;
        }
    }

    private void OnInteractHand(EntityUid uid, EventItemDispenserComponent comp, ref InteractHandEvent args)
    {
        EntityUid user = args.User;

        if (MetaData(user).EntityPrototype?.ID == GameTicker.AdminObserverPrototypeName)
        {
            _popup.PopupEntity("TODO aghost configuring UI", uid, args.User); // todo ditto
            return;
        }
        List<EntityUid> items = comp.dispensedItems.GetOrNew(user).Where(item => !TerminatingOrDeleted(item)).ToList();
        comp.dispensedItems[user] = items;
        comp.dispensedItemsAmount.TryGetValue(user, out int allTimeAmount);

        if (comp.Limit > 0)
        {
            if (!comp.AutoDispose && items.Count >= comp.Limit)
            {
                _audio.PlayPvs(comp.FailSound, uid);
                _popup.PopupEntity(_loc.GetString("event-item-dispenser-limit-reached"), uid, args.User);
                return;
            }

            if (!comp.Infinite && allTimeAmount >= comp.Limit)
            {
                _audio.PlayPvs(comp.FailSound, uid);
                _popup.PopupEntity(_loc.GetString("event-item-dispenser-out-of-stock"), uid, args.User);
                return;
            }
            DeleteExcess(user, comp);
        }
        Dispense(user, comp);
    }

    /// <summary>
    /// Deletes items over limit. Actually, because of the ">=" in the while loop, deletes items over excess plus one. This (somewhat) makes sense
    /// because this method is called immediately before dispensing a new item, if item limit is set.
    /// </summary>
    private void DeleteExcess(EntityUid owner, EventItemDispenserComponent comp)
    {
        var items = comp.dispensedItems[owner];
        int itemAmount = items.Count;
        while (itemAmount >= comp.Limit) // in case limit was substantially decreased at some point.
        {
            var toDelete = items.First();
            if (comp.ReplaceDisposedItems)
            {
                var mapPos = _transform.ToMapCoordinates(new Robust.Shared.Map.EntityCoordinates(toDelete, default));
                Spawn(comp.DisposedReplacement, mapPos);
            }
            QueueDel(toDelete);
            itemAmount--;
            // items.Remove(toDelete); // who cares, invalid EntityUids will be pruned in the beginning of OnActivate anyways
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
        _audio.PlayPvs(comp.DispenseSound, comp.Owner);
    }
}

