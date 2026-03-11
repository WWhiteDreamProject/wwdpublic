using Content.Server._NC.Bank;
using Content.Shared._NC.Doors.Components;
using Content.Shared._NC.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Server.Doors.Systems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Content.Shared.Popups;
using Content.Server.Popups;
using Robust.Shared.Random;
using System;
using Robust.Shared.Localization;

using Content.Server.PDA;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server._NC.Doors.Systems;

public sealed class DoorInterfaceSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly BankSystem _bankSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly PdaSystem _pdaSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!; // NC

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorInterfaceComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DoorInterfaceComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<DoorInterfaceComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<DoorInterfaceComponent, DoorInterfaceBuyMessage>(OnBuy);
        SubscribeLocalEvent<DoorInterfaceComponent, DoorInterfaceSellMessage>(OnSell);
        SubscribeLocalEvent<DoorInterfaceComponent, DoorInterfaceLockMessage>(OnLock);
        SubscribeLocalEvent<DoorInterfaceComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void OnLock(EntityUid uid, DoorInterfaceComponent component, DoorInterfaceLockMessage args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        if (!_playerManager.TryGetSessionByEntity(user, out var session))
            return;

        if (component.OwnerId != session.UserId)
            return;

        ToggleLock(uid);
        UpdateState(uid, component);
    }

    // ...

    private void OnStartup(EntityUid uid, DoorInterfaceComponent component, ComponentStartup args)
    {
        if (!TryComp<DoorComponent>(uid, out var door))
            return;

        _doorSystem.TryClose(uid, door);

        if (component.OwnerId == null)
            SetBolts(uid, true);

        if (string.IsNullOrEmpty(component.DoorCode)) // NC
        {
            component.DoorCode = GenerateDoorCode();
            Dirty(uid, component);
        }
    }

    private string GenerateDoorCode()
    {
        // 2 Letters + 3 Digits
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var l1 = letters[_random.Next(letters.Length)];
        var l2 = letters[_random.Next(letters.Length)];
        var d1 = _random.Next(0, 10);
        var d2 = _random.Next(0, 10);
        var d3 = _random.Next(0, 10);
        return $"{l1}{l2}-{d1}{d2}{d3}"; // NC
    }

    private void OnGetAltVerbs(EntityUid uid, DoorInterfaceComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Power Check
        if (TryComp<Content.Server.Power.Components.ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => _uiSystem.TryToggleUi(uid, DoorInterfaceUiKey.Key, args.User),
            Text = Loc.GetString("door-interface-verb-open"),
            Priority = 1
        });
    }

    private void OnGetVerbs(EntityUid uid, DoorInterfaceComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (component.OwnerId == null)
            return;

        if (!_playerManager.TryGetSessionByEntity(args.User, out var session) || session.UserId != component.OwnerId)
            return;

        // Power Check
        if (TryComp<Content.Server.Power.Components.ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            return;

        args.Verbs.Add(new Verb
        {
            Act = () => ToggleLock(uid),
            Text = Loc.GetString("door-interface-verb-toggle-lock"),
            Priority = 1,
            Category = VerbCategory.Interaction
        });
    }

    private void ToggleLock(EntityUid uid)
    {
        if (TryComp<DoorBoltComponent>(uid, out var bolts))
        {
            _doorSystem.SetBoltsDown((uid, bolts), !bolts.BoltsDown);
            if (TryComp<DoorInterfaceComponent>(uid, out var comp))
                UpdateState(uid, comp);
        }
    }

    private void SetBolts(EntityUid uid, bool state)
    {
        if (TryComp<DoorBoltComponent>(uid, out var bolts))
        {
            _doorSystem.SetBoltsDown((uid, bolts), state);
        }
    }

    private void OnBuy(EntityUid uid, DoorInterfaceComponent component, DoorInterfaceBuyMessage args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        if (!_playerManager.TryGetSessionByEntity(user, out var session))
            return;

        if (component.OwnerId != null) return;

        if (_bankSystem.TryBankWithdraw(user, component.Price))
        {
            component.OwnerId = session.UserId;
            component.OwnerName = Name(user);
            component.Price /= 2;

            _popupSystem.PopupEntity(Loc.GetString("door-interface-popup-bought"), user, user);

            SetBolts(uid, false);
            Dirty(uid, component); // Network fields
            UpdateState(uid, component);

            if (_inventorySystem.TryGetSlotEntity(user, "id", out var pdaUid) && TryComp<PdaComponent>(pdaUid, out var pda)) // NC
            {
                var code = component.DoorCode ?? Loc.GetString("door-interface-pda-unknown");
                _pdaSystem.AddHousing(pdaUid.Value, code, pda);
            }
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("door-interface-popup-insufficient-funds"), user, user);
        }
    }

    private void OnSell(EntityUid uid, DoorInterfaceComponent component, DoorInterfaceSellMessage args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        if (!_playerManager.TryGetSessionByEntity(user, out var session))
            return;

        if (component.OwnerId != session.UserId) return;

        if (_bankSystem.TryBankDeposit(user, component.Price))
        {
            component.OwnerId = null;
            component.OwnerName = null;
            component.Price *= 2;

            _popupSystem.PopupEntity(Loc.GetString("door-interface-popup-sold"), user, user);

            SetBolts(uid, true);
            Dirty(uid, component); // Network fields
            UpdateState(uid, component);

            if (_inventorySystem.TryGetSlotEntity(user, "id", out var pdaUid) && TryComp<PdaComponent>(pdaUid, out var pda)) // NC
            {
                var code = component.DoorCode ?? Loc.GetString("door-interface-pda-unknown");
                _pdaSystem.RemoveHousing(pdaUid.Value, code, pda);
            }
        }
    }

    private void OnUiOpened(EntityUid uid, DoorInterfaceComponent component, BoundUIOpenedEvent args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, DoorInterfaceComponent component)
    {
        var isLocked = false;
        if (TryComp<DoorBoltComponent>(uid, out var bolts))
        {
            isLocked = bolts.BoltsDown;
        }

        var state = new DoorInterfaceState(
            component.Price,
            component.OwnerName,
            component.Address,
            component.OwnerId,
            isLocked,
            component.DoorCode
        );

        _uiSystem.SetUiState(uid, DoorInterfaceUiKey.Key, state);
    }
}
