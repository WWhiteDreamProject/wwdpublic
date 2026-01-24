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
using System;

namespace Content.Server._NC.Doors.Systems;

public sealed class DoorInterfaceSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly BankSystem _bankSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

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

    private void OnStartup(EntityUid uid, DoorInterfaceComponent component, ComponentStartup args)
    {
        if (!TryComp<DoorComponent>(uid, out var door))
            return;

        _doorSystem.TryClose(uid, door);

        if (component.OwnerId == null)
            SetBolts(uid, true);
    }

    private void OnGetAltVerbs(EntityUid uid, DoorInterfaceComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => _uiSystem.TryToggleUi(uid, DoorInterfaceUiKey.Key, args.User),
            Text = "Открыть интерфейс",
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

        args.Verbs.Add(new Verb
        {
            Act = () => ToggleLock(uid),
            Text = "Блокировать/Разблокировать",
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

            _popupSystem.PopupEntity("Вы приобрели недвижимость!", user, user);

            SetBolts(uid, false);
            UpdateState(uid, component);
        }
        else
        {
            _popupSystem.PopupEntity("Недостаточно средств!", user, user);
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

            _popupSystem.PopupEntity("Вы продали недвижимость.", user, user);

            SetBolts(uid, true);
            UpdateState(uid, component);
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
            isLocked
        );

        _uiSystem.SetUiState(uid, DoorInterfaceUiKey.Key, state);
    }
}
