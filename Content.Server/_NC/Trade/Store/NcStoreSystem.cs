using System.Numerics;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared._NC.Trade;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Movement.Pulling.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;


namespace Content.Server._NC.Trade;


public sealed class NcStoreSystem : EntitySystem
{
    private const float MaxUseDistance = 2.5f;
    private const float MaxCrateDistance = 4f;
    private static readonly ISawmill Sawmill = Logger.GetSawmill("ncstore");
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly NcStoreLogicSystem _logic = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly StoreStructuredSystem _storeUi = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NcStoreComponent, StoreBuyListingBoundUiMessage>(OnBuyRequest);
        SubscribeLocalEvent<NcStoreComponent, StoreSellListingBoundUiMessage>(OnSellRequest);
        SubscribeLocalEvent<NcStoreComponent, StoreMassSellPulledCrateBoundUiMessage>(OnMassSellPulledCrateRequest);
    }

    public bool CanUseStore(EntityUid store, NcStoreComponent comp, EntityUid user)
    {
        if (!Exists(user))
            return false;

        if (TryComp(store, out AccessReaderComponent? reader))
        {
            if (!_accessReader.IsAllowed(user, store, reader))
                return false;
        }

        return true;
    }

    private bool IsInRange(EntityUid a, EntityUid b, float maxDistance)
    {
        if (!_entMan.TryGetComponent(a, out TransformComponent? aXf))
            return false;
        if (!_entMan.TryGetComponent(b, out TransformComponent? bXf))
            return false;

        if (aXf.MapID != bXf.MapID)
            return false;

        var aPos = _transform.GetWorldPosition(aXf);
        var bPos = _transform.GetWorldPosition(bXf);

        return Vector2.Distance(aPos, bPos) <= maxDistance;
    }

    private bool IsInUseRange(EntityUid store, EntityUid user) => IsInRange(store, user, MaxUseDistance);


    private bool TryValidateUse(EntityUid store, NcStoreComponent comp, EntityUid actor, out string failMessage)
    {
        failMessage = string.Empty;

        if (!CanUseStore(store, comp, actor))
        {
            failMessage = Loc.GetString("nc-store-popup-no-access");
            return false;
        }

        if (!IsInUseRange(store, actor))
        {
            failMessage = Loc.GetString("nc-store-popup-too-far");
            return false;
        }

        return true;
    }

    private void EnsureListingIndex(EntityUid store, NcStoreComponent comp)
    {
        if (comp.Listings.Count > 0 && comp.ListingIndex.Count == 0)
        {
            Sawmill.Error($"[NcStore] {ToPrettyString(store)} has listings but empty ListingIndex. Rebuilding.");
            comp.RebuildListingIndex();
        }
    }

    private bool TryGetListing(
        EntityUid store,
        NcStoreComponent comp,
        EntityUid actor,
        StoreMode mode,
        string id,
        out NcStoreListingDef listing
    )
    {
        listing = default!;

        EnsureListingIndex(store, comp);

        if (!comp.ListingIndex.TryGetValue(NcStoreComponent.MakeListingKey(mode, id), out var found))
        {
            Sawmill.Warning(
                $"[NcStore] {ToPrettyString(actor)} tried to use invalid listing '{id}' (mode={mode}) at {ToPrettyString(store)}");
            return false;
        }

        listing = found;
        return true;
    }

    private bool TryGetPulledClosedCrate(EntityUid actor, out EntityUid crate, out string failMessage)
    {
        crate = default;
        failMessage = string.Empty;

        if (_logic.TryGetPulledClosedCrate(actor, out crate))
            return true;

        if (_entMan.TryGetComponent(actor, out PullerComponent? puller) &&
            puller.Pulling is { } pulled &&
            _entMan.TryGetComponent(pulled, out EntityStorageComponent? storage) &&
            storage.Open)
        {
            failMessage = Loc.GetString("nc-store-popup-crate-open");
            return false;
        }

        failMessage = Loc.GetString("nc-store-popup-no-crate");
        return false;
    }


    private void PopupFail(EntityUid actor, string message) => _popups.PopupEntity(message, actor, actor);


    private bool TryGetLockedUiUser(EntityUid store, NcStoreComponent comp, out EntityUid user)
    {
        user = default;
        if (comp.CurrentUser is not { } cur || cur == EntityUid.Invalid)
            return false;
        if (!_ui.IsUiOpen(store, StoreUiKey.Key, cur))
            return false;

        user = cur;
        return true;
    }


    private void OnBuyRequest(EntityUid uid, NcStoreComponent comp, StoreBuyListingBoundUiMessage msg)
    {
        if (!TryGetLockedUiUser(uid, comp, out var actor))
            return;

        if (!TryValidateUse(uid, comp, actor, out var fail))
        {
            PopupFail(actor, fail);
            return;
        }

        if (!TryGetListing(uid, comp, actor, StoreMode.Buy, msg.Id, out var listing))
        {
            PopupFail(actor, Loc.GetString("nc-store-popup-invalid-listing"));
            return;
        }

        var count = Math.Max(1, msg.Count);
        if (!_logic.TryBuy(listing.Id, uid, comp, actor, count))
        {
            PopupFail(actor, Loc.GetString("nc-store-popup-transaction-failed"));
            return;
        }

        _audio.PlayPvs("/Audio/Effects/Cargo/ping.ogg", uid, AudioParams.Default.WithVolume(-2f));
        _storeUi.UpdateDynamicState(uid, comp, actor);
    }

    private void OnSellRequest(EntityUid uid, NcStoreComponent comp, StoreSellListingBoundUiMessage msg)
    {
        if (!TryGetLockedUiUser(uid, comp, out var actor))
            return;

        if (!TryValidateUse(uid, comp, actor, out var fail))
        {
            PopupFail(actor, fail);
            return;
        }

        var requestedId = msg.Id;
        var fromCrate = msg.FromCrate;
        if (string.IsNullOrEmpty(requestedId))
            return;

        if (!TryGetListing(uid, comp, actor, StoreMode.Sell, requestedId, out var listing))
        {
            PopupFail(actor, Loc.GetString("nc-store-popup-invalid-listing"));
            return;
        }


        var count = Math.Max(1, msg.Count);

        bool ok;

        if (fromCrate)
        {
            if (!TryGetPulledClosedCrate(actor, out var crate, out var crateFail))
            {
                PopupFail(actor, crateFail);
                return;
            }

            if (!IsInRange(uid, crate, MaxCrateDistance))
            {
                PopupFail(actor, Loc.GetString("nc-store-popup-crate-too-far"));
                return;
            }

            ok = _logic.TrySellFromContainer(listing.Id, uid, comp, actor, crate, count);
        }
        else
            ok = _logic.TrySell(listing.Id, uid, comp, actor, count);

        if (!ok)
        {
            PopupFail(actor, Loc.GetString("nc-store-popup-transaction-failed"));
            return;
        }

        _audio.PlayPvs("/Audio/Effects/Cargo/ping.ogg", uid, AudioParams.Default.WithVolume(-2f));
        _storeUi.UpdateDynamicState(uid, comp, actor);
    }


    private void OnMassSellPulledCrateRequest(
        EntityUid uid,
        NcStoreComponent comp,
        StoreMassSellPulledCrateBoundUiMessage msg
    )
    {
        if (!TryGetLockedUiUser(uid, comp, out var actor))
            return;

        if (!TryValidateUse(uid, comp, actor, out var fail))
        {
            PopupFail(actor, fail);
            return;
        }

        if (!TryGetPulledClosedCrate(actor, out var crate, out var crateFail))
        {
            PopupFail(actor, crateFail);
            return;
        }

        if (!IsInRange(uid, crate, MaxCrateDistance))
        {
            PopupFail(actor, Loc.GetString("nc-store-popup-crate-too-far"));
            return;
        }

        if (!_logic.TryMassSellFromContainer(uid, comp, actor, crate))
        {
            PopupFail(actor, Loc.GetString("nc-store-popup-transaction-failed"));
            return;
        }

        _audio.PlayPvs("/Audio/Effects/Cargo/ping.ogg", uid, AudioParams.Default.WithVolume(-2f));
        _storeUi.UpdateDynamicState(uid, comp, actor);
    }
}
