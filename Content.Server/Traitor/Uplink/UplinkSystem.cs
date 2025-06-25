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
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly RingerSystem _ringerSystem = default!;

    [ValidatePrototypeId<CurrencyPrototype>]
    public const string TelecrystalCurrencyPrototype = "Telecrystal";
    private const string FallbackUplinkImplant = "UplinkImplant";
    // This catalog is commented out in uplink_catalog.yml, using another one
    // private const string FallbackUplinkCatalog = "UplinkUplinkImplanter";
    private const string FallbackUplinkCatalog = "UplinkMicrobomb"; // Using a prototype that definitely exists
    private const string OldFashionedRadioUplink = "BaseUplinkRadio";

    /// <summary>
    /// Adds an uplink to the target
    /// </summary>
    /// <param name="user">The person who is getting the uplink</param>
    /// <param name="balance">The amount of currency on the uplink. If null, will just use the amount specified in the preset.</param>
    /// <param name="uplinkEntity">The entity that will actually have the uplink functionality. If null, will use uplinkPref to create a new one.</param>
    /// <param name="uplinkPref">The preferred uplink type, used if uplinkEntity is null.</param>
    /// <param name="giveDiscounts">Marker that enables discounts for uplink items.</param>
    /// <returns>Whether or not the uplink was added successfully</returns>
    public bool AddUplink(
        EntityUid user,
        FixedPoint2 balance,
        EntityUid? uplinkEntity = null,
        UplinkPreference uplinkPref = UplinkPreference.PDA, // Default to PDA as a safe bet
        bool giveDiscounts = false)
    {
        // If a specific entity is provided for the uplink, use it.
        if (uplinkEntity != null)
        {
            Logger.Debug($"AddUplink: Using provided uplink entity {uplinkEntity}");
            if (HasComp<PdaComponent>(uplinkEntity.Value) && !HasComp<RingerUplinkComponent>(uplinkEntity.Value))
            {
                var ringerUplink = EnsureComp<RingerUplinkComponent>(uplinkEntity.Value);
                _ringerSystem.RandomizeUplinkCode(uplinkEntity.Value, ringerUplink, new());
            }
            EnsureComp<UplinkComponent>(uplinkEntity.Value);
            SetUplink(user, uplinkEntity.Value, balance, giveDiscounts);
            return true;
        }

        // Otherwise, create an uplink based on user preference.
        Logger.Debug($"AddUplink: Processing uplink preference {uplinkPref}");
        switch (uplinkPref)
        {
            case UplinkPreference.PDA:
                var pdaEntity = FindUplinkTarget(user);
                if (pdaEntity != null)
                {
                    Logger.Debug($"AddUplink: Found PDA {pdaEntity} for user preference");
                    if (HasComp<PdaComponent>(pdaEntity.Value) && !HasComp<RingerUplinkComponent>(pdaEntity.Value))
                    {
                        var ringerUplink = EnsureComp<RingerUplinkComponent>(pdaEntity.Value);
                        _ringerSystem.RandomizeUplinkCode(pdaEntity.Value, ringerUplink, new());
                    }
                    EnsureComp<UplinkComponent>(pdaEntity.Value);
                    SetUplink(user, pdaEntity.Value, balance, giveDiscounts);
                    return true;
                }
                Logger.Debug($"AddUplink: No PDA found despite preference, falling back to implant.");
                return ImplantUplink(user, balance, giveDiscounts);

            case UplinkPreference.Implant:
                Logger.Debug($"AddUplink: Creating implant uplink per user preference.");
                return ImplantUplink(user, balance, giveDiscounts);

            case UplinkPreference.Radio:
            default:
                Logger.Debug($"AddUplink: Creating radio uplink for user {user}.");
                if (!_proto.HasIndex<EntityPrototype>(OldFashionedRadioUplink))
                {
                    Logger.Error($"AddUplink: Radio uplink prototype {OldFashionedRadioUplink} not found. Falling back to implant.");
                    return ImplantUplink(user, balance, giveDiscounts);
                }

                var radioUplink = EntityManager.SpawnEntity(OldFashionedRadioUplink, Transform(user).Coordinates);
                var handEnt = _handsSystem.GetActiveHand(user);
                var freeHand = _handsSystem.EnumerateHands(user).FirstOrDefault(hand => hand.HeldEntity == null);

                if (freeHand != null)
                {
                    _handsSystem.TryPickup(user, radioUplink, freeHand);
                }
                else if (handEnt != null && _handsSystem.TryDrop(user, handEnt, checkActionBlocker: false))
                {
                    _handsSystem.TryPickup(user, radioUplink);
                }

                EnsureComp<UplinkComponent>(radioUplink);
                SetUplink(user, radioUplink, balance, giveDiscounts);
                return true;
        }
    }

    /// <summary>
    /// Configure TC for the uplink
    /// </summary>
    private void SetUplink(EntityUid user, EntityUid uplink, FixedPoint2 balance, bool giveDiscounts)
    {
        var store = EnsureComp<StoreComponent>(uplink);
        store.AccountOwner = user;

        store.Balance.Clear();
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { TelecrystalCurrencyPrototype, balance } },
            uplink,
            store);

        var uplinkInitializedEvent = new StoreInitializedEvent(
            TargetUser: user,
            Store: uplink,
            UseDiscounts: giveDiscounts,
            Listings: _store.GetAvailableListings(user, uplink, store)
                .ToArray());
        RaiseLocalEvent(ref uplinkInitializedEvent);
    }

    /// <summary>
    /// Implant an uplink as a fallback measure if the traitor had no PDA
    /// </summary>
    private bool ImplantUplink(EntityUid user, FixedPoint2 balance, bool giveDiscounts)
    {
        Logger.Debug($"ImplantUplink: Creating implant uplink for user {user}");

        // Create implant directly
        var implantProto = new string(FallbackUplinkImplant);

        // Check if implant prototype exists
        if (!_proto.HasIndex<EntityPrototype>(implantProto))
        {
            Logger.Error($"ImplantUplink: Implant prototype {implantProto} not found");
            return false;
        }

        // Create implant directly
        var implant = _subdermalImplant.AddImplant(user, implantProto);

        if (implant == null)
        {
            Logger.Error($"ImplantUplink: Failed to create implant for user {user}");
            return false;
        }

        if (!HasComp<StoreComponent>(implant))
        {
            Logger.Error($"ImplantUplink: Implant {implant} does not have StoreComponent");
            return false;
        }

        Logger.Debug($"ImplantUplink: Successfully created implant {implant} for user {user}");
        SetUplink(user, implant.Value, balance, giveDiscounts);
        return true;
    }

    /// <summary>
    /// Finds the entity that can hold an uplink for a user.
    /// Usually this is a pda in their pda slot, but can also be in their hands. (but not pockets or inside bag, etc.)
    /// </summary>
    public EntityUid? FindUplinkTarget(EntityUid user)
    {
        Logger.Debug($"FindUplinkTarget: Looking for uplink target for user {user}");

        // Try to find PDA in inventory
        if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.MoveNext(out var pdaUid))
            {
                if (!pdaUid.ContainedEntity.HasValue)
                    continue;

                var entity = pdaUid.ContainedEntity.Value;

                if (HasComp<PdaComponent>(entity))
                {
                    Logger.Debug($"FindUplinkTarget: Found PDA {entity} in inventory");
                    return entity;
                }

                if (HasComp<StoreComponent>(entity))
                {
                    Logger.Debug($"FindUplinkTarget: Found store component {entity} in inventory");
                    return entity;
                }
            }
        }

        // Also check hands
        foreach (var item in _handsSystem.EnumerateHeld(user))
        {
            if (HasComp<PdaComponent>(item))
            {
                Logger.Debug($"FindUplinkTarget: Found PDA {item} in hands");
                return item;
            }

            if (HasComp<StoreComponent>(item))
            {
                Logger.Debug($"FindUplinkTarget: Found store component {item} in hands");
                return item;
            }
        }

        Logger.Debug($"FindUplinkTarget: No suitable uplink target found for user {user}");
        return null;
    }
}
