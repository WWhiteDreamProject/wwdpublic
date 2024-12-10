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
    [Dependency] private readonly ISawmill _sawmill = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;



    public override void Initialize()
    {
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
        comp.DispensingPrototype = ValidateProto(msg.DispensingPrototype, "FoodBanana");
        comp.AutoDispose = msg.AutoDispose;
        comp.CanManuallyDispose = msg.CanManuallyDispose;
        comp.Infinite = msg.Infinite;
        comp.Limit = msg.Limit;
        
        if(string.IsNullOrWhiteSpace(msg.DisposedReplacement)) // if the prototype is set to null, just set the relevant flag to false
        {
            comp.ReplaceDisposedItems = false;
        }
        else
        {
            comp.DisposedReplacement = ValidateProto(msg.DisposedReplacement, "EffectTeslaSparksSilent");
            comp.ReplaceDisposedItems = msg.ReplaceDisposedItems;
        }
        comp.AutoCleanUp = msg.AutoCleanUp;
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
            foreach (var items in comp.dispensedItems.Values)
            {
                foreach (var item in items)
                {
                    if (!TerminatingOrDeleted(item)) // do i have to?
                    {
                        QueueDel(item); // no fancy effects
                    } // "{{{{}}}}" is non-negotiable
                }
            }
        }
    }
    private void OnInteractUsing(EntityUid uid, EventItemDispenserComponent comp, InteractUsingEvent args)
    {
        EntityUid user = args.User;

        if (CanOpenUI(user))
        {
            string newProto = MetaData(args.Used).EntityPrototype?.ID ?? comp.DispensingPrototype;
            if(_ui.TryOpenUi((uid, comp), EventItemDispenserUiKey.Key, user))
                _ui.ServerSendUiMessage((uid, comp), EventItemDispenserUiKey.Key, new EventItemDispenserNewProtoBoundUserInterfaceMessage(newProto), args.User);
            return;
        }
        if( comp.CanManuallyDispose &&
            TryComp<EventDispensedComponent>(args.Used, out var dispensed) &&
            dispensed.ItemOwner == user &&
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

        if (CanOpenUI(user))
        {
            _ui.TryToggleUi((uid, comp), EventItemDispenserUiKey.Key, user); // todo: user is ICommonSession, not EntityUid; tuple is not converted into Entity (it should)
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
                _popup.PopupEntity(_loc.GetString("event-item-dispenser-limit-reached"), uid, user);
                return;
            }

            if (!comp.Infinite && allTimeAmount >= comp.Limit)
            {
                _audio.PlayPvs(comp.FailSound, uid);
                _popup.PopupEntity(_loc.GetString("event-item-dispenser-out-of-stock"), uid, user);
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

