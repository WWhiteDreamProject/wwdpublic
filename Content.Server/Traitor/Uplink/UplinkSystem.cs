using System.Linq;
using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.PDA.Ringer;

namespace Content.Server.Traitor.Uplink;

public sealed class UplinkSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;
// WWDP edit start
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly RingerSystem _ringerSystem = default!;
// WWDP edit end

    [ValidatePrototypeId<CurrencyPrototype>]
    public const string TelecrystalCurrencyPrototype = "Telecrystal";

    private const string UplinkImplantPrototype = "UplinkImplant"; // WWDP edit
    private const string RadioUplinkPrototype = "BaseUplinkRadio"; // WWDP edit

    /// <summary>
    /// Adds an uplink to the target
    /// </summary>
    /// <param name="user">The entity receiving the uplink.</param>
    /// <param name="balance">The starting balance for the uplink.</param>
    /// <param name="uplinkEntity">An optional entity to use as the uplink. If null, one will be created based on preference.</param>
    /// <param name="uplinkPref">The preferred uplink type, used if uplinkEntity is null.</param>
    /// <param name="giveDiscounts">Whether to apply discounts to the store.</param>
    /// <returns>True if the uplink was added successfully, false otherwise.</returns>
    public bool AddUplink(
        EntityUid user,
        FixedPoint2 balance,
        EntityUid? uplinkEntity = null,
        UplinkPreference uplinkPref = UplinkPreference.PDA, // WWDP add
        bool giveDiscounts = false)
    {
        // TODO add BUI. Currently can't be done outside of yaml -_-
        // ^ What does this even mean?
// WWDP edit start
        if (uplinkEntity != null)
        {
            SetupUplink(user, uplinkEntity.Value, balance, giveDiscounts);
            return true;
        }

        return TryCreateUplink(user, balance, uplinkPref, giveDiscounts);
    }

    private void SetupUplink(EntityUid user, EntityUid uplink, FixedPoint2 balance, bool giveDiscounts)
    {
        if (HasComp<PdaComponent>(uplink) && !HasComp<RingerUplinkComponent>(uplink))
        {
            var ringerUplink = EnsureComp<RingerUplinkComponent>(uplink);
            _ringerSystem.RandomizeUplinkCode(uplink, ringerUplink, new());
        }
        EnsureComp<UplinkComponent>(uplink);

        var store = EnsureComp<StoreComponent>(uplink);
        store.AccountOwner = user;
        store.Balance.Clear();
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { TelecrystalCurrencyPrototype, balance } }, uplink, store);

        var initializedEvent = new StoreInitializedEvent(user, uplink, giveDiscounts, _store.GetAvailableListings(user, uplink, store).ToArray());
        RaiseLocalEvent(ref initializedEvent);
    }

    private bool TryCreateUplink(EntityUid user, FixedPoint2 balance, UplinkPreference uplinkPref, bool giveDiscounts)
    {
        bool success;
        switch (uplinkPref)
        {
            case UplinkPreference.PDA:
                success = TryCreatePdaUplink(user, balance, giveDiscounts);
                break;
            case UplinkPreference.Implant:
                success = TryCreateImplantUplink(user, balance, giveDiscounts);
                break;
            case UplinkPreference.Radio:
                success = TryCreateRadioUplink(user, balance, giveDiscounts);
                break;
            default:
                Logger.Error($"Unsupported uplink preference: {uplinkPref}. No uplink will be given.");
                return false;
        }

        if (!success)
        {
            Logger.Info($"Could not create preferred uplink ({uplinkPref}) for {ToPrettyString(user)}. No uplink was given.");
        }
        
        return success;
    }

    private bool TryCreatePdaUplink(EntityUid user, FixedPoint2 balance, bool giveDiscounts)
    {
        var pdaEntity = FindUplinkTarget(user);
        if (pdaEntity != null)
        {
            SetupUplink(user, pdaEntity.Value, balance, giveDiscounts);
            return true;
        }
        return false;
    }

    private bool TryCreateImplantUplink(EntityUid user, FixedPoint2 balance, bool giveDiscounts)
    {
        if (!_proto.HasIndex<EntityPrototype>(UplinkImplantPrototype))
        {
            Logger.Error($"Implant prototype {UplinkImplantPrototype} not found.");
            return false;
        }

        var implant = _subdermalImplant.AddImplant(user, UplinkImplantPrototype);
        if (implant == null)
        {
            Logger.Error($"Failed to create implant uplink for user {user}.");
            return false;
        }

        if (!HasComp<StoreComponent>(implant))
        {
            Logger.Error($"Implant {implant} does not have a StoreComponent.");
            // We probably want to delete the implant here to not leave a useless implant.
            QueueDel(implant.Value);
            return false;
        }

        SetupUplink(user, implant.Value, balance, giveDiscounts);
        return true;
    }

    private bool TryCreateRadioUplink(EntityUid user, FixedPoint2 balance, bool giveDiscounts)
    {
        if (!_proto.HasIndex<EntityPrototype>(RadioUplinkPrototype))
        {
            Logger.Error($"Radio uplink prototype {RadioUplinkPrototype} not found.");
            return false;
        }

        var radioUplink = EntityManager.SpawnEntity(RadioUplinkPrototype, Transform(user).Coordinates);
        _handsSystem.TryPickupAnyHand(user, radioUplink, checkActionBlocker: false);

        SetupUplink(user, radioUplink, balance, giveDiscounts);
        return true;
    }

    /// <summary>
    /// Finds a suitable entity to host an uplink for a user.
    /// Prefers a PDA in the user's PDA slot, then checks hands.
    /// </summary>
    public EntityUid? FindUplinkTarget(EntityUid user)
    {
        // Check inventory first, this is recursively checked so it will find PDAs in containers.
        if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.MoveNext(out var pdaUid))
            {
                if (pdaUid.ContainedEntity.HasValue && IsValidUplinkTarget(pdaUid.ContainedEntity))
                {
                    return pdaUid.ContainedEntity.Value;
                }
            }
        }

        // Then check hands
        foreach (var item in _handsSystem.EnumerateHeld(user))
        {
            if (IsValidUplinkTarget(item))
            {
                return item;
            }
        }

        return null;
    }

    private bool IsValidUplinkTarget(EntityUid? entity)
    {
        return entity.HasValue && HasComp<PdaComponent>(entity.Value);
    }
// WWDP edit end
}
