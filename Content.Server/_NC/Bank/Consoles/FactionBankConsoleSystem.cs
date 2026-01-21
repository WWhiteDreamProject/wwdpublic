using Content.Server._NC.Bank;
using Content.Server.Station.Systems;
using Content.Shared._NC.Bank.Consoles;
using Content.Shared._NC.Bank;
using Content.Shared._NC.Bank.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Content.Server.Stack;
using Robust.Shared.Timing;

namespace Content.Server._NC.Bank.Consoles
{
    public sealed class FactionBankConsoleSystem : EntitySystem
    {
        [Dependency] private readonly BankSystem _bankSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public const string CashSlotId = "cash_slot";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FactionBankConsoleComponent, BoundUIOpenedEvent>(OnUiOpen);
            SubscribeLocalEvent<FactionBankConsoleComponent, FactionBankWithdrawMessage>(OnWithdraw);
            SubscribeLocalEvent<FactionBankConsoleComponent, FactionBankDepositMessage>(OnDeposit);
            SubscribeLocalEvent<FactionBankConsoleComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnUiOpen(EntityUid uid, FactionBankConsoleComponent component, BoundUIOpenedEvent args)
        {
            UpdateUi(uid, component);
        }

        private void OnInteractUsing(EntityUid uid, FactionBankConsoleComponent component, InteractUsingEvent args)
        {
            if (!TryComp<Content.Shared.Stacks.StackComponent>(args.Used, out var stack) ||
               MetaData(args.Used).EntityPrototype?.ID != "SpaceCash") return;

            if (_containerSystem.TryGetContainer(uid, CashSlotId, out var cashContainer))
            {
                if (_containerSystem.Insert(args.Used, cashContainer))
                {
                    args.Handled = true;
                    // Auto-update UI logic could go here if we showed "Inserted Amount"
                }
            }
        }

        private void UpdateUi(EntityUid uid, FactionBankConsoleComponent component)
        {
            var station = _stationSystem.GetOwningStation(uid);
            var balance = 0;
            var logs = new List<BankTransaction>();

            if (station != null && component.BankAccount != SectorBankAccount.Invalid)
            {
                if (TryComp<StationBankComponent>(station, out var stationBank) &&
                    stationBank.Accounts.TryGetValue(component.BankAccount, out var info))
                {
                    balance = info.Balance;
                    logs = info.Logs;
                }
            }

            var title = component.BankAccount.ToString();
            _uiSystem.SetUiState(uid, FactionBankConsoleUiKey.Key, new FactionBankConsoleState(balance, title, logs));
        }

        private void OnWithdraw(EntityUid uid, FactionBankConsoleComponent component, FactionBankWithdrawMessage args)
        {
            if (args.Amount <= 0) return;
            if (args.Actor is not { Valid: true } player) return;

            if (!_accessReader.IsAllowed(player, uid))
            {
                _popupSystem.PopupEntity("Доступ запрещен!", uid, player);
                return;
            }

            var station = _stationSystem.GetOwningStation(uid);
            if (station == null) return;

            if (_bankSystem.TryFactionWithdraw(station.Value, component.BankAccount, args.Amount))
            {
                _stackSystem.SpawnMultiple("SpaceCash", args.Amount, Transform(uid).Coordinates);
                _popupSystem.PopupEntity($"Снято {args.Amount} эдди. {args.Description}", uid, player);

                // Add Log
                AddLog(station.Value, component.BankAccount, new BankTransaction(
                    _timing.CurTime, Name(player), BankTransactionType.Withdraw, args.Amount, args.Description));

                UpdateUi(uid, component);
            }
            else
            {
                _popupSystem.PopupEntity("Недостаточно средств на счете организации.", uid, player);
            }
        }

        private void OnDeposit(EntityUid uid, FactionBankConsoleComponent component, FactionBankDepositMessage args)
        {
            if (args.Amount <= 0) return;
            if (args.Actor is not { Valid: true } player) return;

            var station = _stationSystem.GetOwningStation(uid);
            if (station == null) return;

            if (!_containerSystem.TryGetContainer(uid, CashSlotId, out var cashContainer) ||
                cashContainer.ContainedEntities.Count == 0)
            {
                _popupSystem.PopupEntity("Вставьте наличные!", uid, player);
                return;
            }

            // Calculate total inserted
            int totalInserted = 0;
            var toDelete = new List<EntityUid>();
            foreach (var item in cashContainer.ContainedEntities)
            {
                if (TryComp<Content.Shared.Stacks.StackComponent>(item, out var stack))
                {
                    totalInserted += _stackSystem.GetCount(item, stack);
                    toDelete.Add(item);
                }
            }

            if (totalInserted < args.Amount)
            {
                _popupSystem.PopupEntity($"Недостаточно наличных! Вставлено: {totalInserted}, Требуется: {args.Amount}", uid, player);
                return;
            }

            // Execute Deposit
            if (_bankSystem.TryFactionDeposit(station.Value, component.BankAccount, args.Amount))
            {
                // Delete all inserted cash
                foreach (var item in toDelete) QueueDel(item);

                // Return Change
                int change = totalInserted - args.Amount;
                if (change > 0)
                {
                    _stackSystem.SpawnMultiple("SpaceCash", change, Transform(uid).Coordinates);
                    _popupSystem.PopupEntity($"Сдача: {change} эдди.", uid, player);
                }

                _popupSystem.PopupEntity($"Внесено {args.Amount} эдди. {args.Description}", uid, player);

                // Add Log
                AddLog(station.Value, component.BankAccount, new BankTransaction(
                   _timing.CurTime, Name(player), BankTransactionType.Deposit, args.Amount, args.Description));

                UpdateUi(uid, component);
            }
        }

        private void AddLog(EntityUid stationUid, SectorBankAccount account, BankTransaction log)
        {
            if (TryComp<StationBankComponent>(stationUid, out var bank) &&
                bank.Accounts.TryGetValue(account, out var info))
            {
                info.Logs.Add(log);
                // Limit logs size??
                if (info.Logs.Count > 50) info.Logs.RemoveAt(0);
            }
        }
    }
}
