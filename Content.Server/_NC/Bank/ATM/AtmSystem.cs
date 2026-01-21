using Content.Server.Stack;
using Content.Server.Popups;
using Content.Shared._NC.Bank.Components;
using Content.Server._NC.Bank; // Ваша BankSystem
using Content.Shared._NC.Bank;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Server.Station.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Runtime.InteropServices;

namespace Content.Server._NC.Bank.ATM
{
    public sealed class AtmSystem : EntitySystem
    {
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;

        // Работаем с вашей системой БД
        [Dependency] private readonly BankSystem _bankSystem = default!;

        private const string CurrencyPrototypeId = "SpaceCash";
        private const string CurrencyStackId = "Credit";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AtmComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<AtmComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<AtmComponent, EntRemovedFromContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<AtmComponent, AtmWithdrawMessage>(OnWithdraw);
            SubscribeLocalEvent<AtmComponent, AtmDepositMessage>(OnDeposit);
            SubscribeLocalEvent<AtmComponent, BoundUIOpenedEvent>(OnUiOpened);
        }

        // === ВСТАВКА ДЕНЕГ РУКАМИ ===
        private void OnInteractUsing(EntityUid uid, AtmComponent component, InteractUsingEvent args)
        {
            if (!TryComp<StackComponent>(args.Used, out var stack) ||
                stack.StackTypeId != CurrencyStackId) return;

            if (_containerSystem.TryGetContainer(uid, AtmComponent.CashSlotId, out var cashContainer))
            {
                if (_containerSystem.Insert(args.Used, cashContainer))
                {
                    args.Handled = true;
                    UpdateUi(uid, component);
                }
            }
        }

        private void OnContainerModified(EntityUid uid, AtmComponent component, ContainerModifiedMessage args) => UpdateUi(uid, component);
        private void OnUiOpened(EntityUid uid, AtmComponent component, BoundUIOpenedEvent args) => UpdateUi(uid, component);

        // === СНЯТИЕ (ИЗ БД ИГРОКА) ===
        private void OnWithdraw(EntityUid uid, AtmComponent component, AtmWithdrawMessage args)
        {
            if (args.Actor is not { Valid: true } player) return;
            if (args.Amount <= 0) return;

            // 1. Просто проверяем, что карта есть (как ключ)
            if (!IsIdCardInserted(uid))
            {
                _popupSystem.PopupEntity("Вставьте карту для авторизации!", uid, player);
                return;
            }

            // 2. Списываем деньги у ИГРОКА (args.Actor) через BankSystem
            if (_bankSystem.TryBankWithdraw(player, args.Amount))
            {
                var cash = Spawn(CurrencyPrototypeId, Transform(uid).Coordinates);
                _stackSystem.SetCount(cash, args.Amount);

                _popupSystem.PopupEntity($"Выдано {args.Amount}$", uid, player);
                UpdateUi(uid, component);
            }
            else
            {
                _popupSystem.PopupEntity("Недостаточно средств на счете!", uid, player);
            }
        }

        // === ВНЕСЕНИЕ (В БД ИГРОКА) ===
        private void OnDeposit(EntityUid uid, AtmComponent component, AtmDepositMessage args)
        {
            if (args.Actor is not { Valid: true } player) return;

            // 1. Проверяем наличие карты
            if (!IsIdCardInserted(uid))
            {
                _popupSystem.PopupEntity("Вставьте карту!", uid, player);
                return;
            }

            // 2. Берем деньги из лотка
            if (!_containerSystem.TryGetContainer(uid, AtmComponent.CashSlotId, out var cashContainer) ||
                cashContainer.ContainedEntities.Count == 0) return;

            var item = cashContainer.ContainedEntities[0];
            if (!TryComp<StackComponent>(item, out var stack)) return;

            // 3. Считаем
            int totalAmount = _stackSystem.GetCount(item, stack);
            int tax = (int) (totalAmount * component.TaxRate);
            int finalDeposit = totalAmount - tax;

            if (finalDeposit <= 0)
            {
                _popupSystem.PopupEntity("Сумма слишком мала.", uid, player);
                return;
            }

            // 4. Зачисляем ИГРОКУ (args.Actor)
            if (_bankSystem.TryBankDeposit(player, finalDeposit))
            {
                PayTaxToStation(uid, tax);

                _popupSystem.PopupEntity($"Зачислено: {finalDeposit}$ (Налог: {tax}$)", uid, player);
                QueueDel(item);
                UpdateUi(uid, component);
            }
        }

        private void PayTaxToStation(EntityUid atmUid, int taxAmount)
        {
            if (taxAmount <= 0) return;
            var stationUid = _stationSystem.GetOwningStation(atmUid);

            if (stationUid != null && TryComp<StationBankComponent>(stationUid, out var stationBank))
            {
                ref var cityAccount = ref CollectionsMarshal.GetValueRefOrNullRef(stationBank.Accounts, SectorBankAccount.CityAdmin);
                if (!System.Runtime.CompilerServices.Unsafe.IsNullRef(ref cityAccount))
                {
                    cityAccount.Balance += taxAmount;
                    Dirty(stationUid.Value, stationBank);
                }
            }
        }

        private void UpdateUi(EntityUid uid, AtmComponent component)
        {
            var user = _uiSystem.GetActors(uid, AtmUiKey.Key).FirstOrDefault();
            if (user == default) return;

            UpdateUiForUser(uid, component, user);
        }

        private void UpdateUiForUser(EntityUid uid, AtmComponent component, EntityUid user)
        {
            string accountName = "Нет карты";
            int balance = 0;
            bool isCardInserted = false;
            int depositAmount = 0;

            if (IsIdCardInserted(uid))
            {
                isCardInserted = true;
                if (TryGetIdCardEntity(uid, out var cardEntity))
                    accountName = Name(cardEntity);

                // ВАЖНО: Получаем баланс из BankSystem (из профиля игрока)
                balance = _bankSystem.GetBalance(user);
            }

            if (_containerSystem.TryGetContainer(uid, AtmComponent.CashSlotId, out var cashContainer) &&
                cashContainer.ContainedEntities.Count > 0)
            {
                var item = cashContainer.ContainedEntities[0];
                if (TryComp<StackComponent>(item, out var stack) &&
                    stack.StackTypeId == CurrencyStackId)
                {
                    depositAmount = _stackSystem.GetCount(item, stack);
                }
            }

            var state = new AtmBoundUserInterfaceState(
                balance,
                accountName,
                isCardInserted,
                component.TaxRate,
                depositAmount
            );

            _uiSystem.SetUiState(uid, AtmUiKey.Key, state);
        }

        private bool IsIdCardInserted(EntityUid uid) => TryGetIdCardEntity(uid, out _);

        private bool TryGetIdCardEntity(EntityUid uid, out EntityUid card)
        {
            card = EntityUid.Invalid;
            if (_containerSystem.TryGetContainer(uid, AtmComponent.IdSlotId, out var container) &&
                container.ContainedEntities.Count > 0)
            {
                card = container.ContainedEntities[0];
                return true;
            }
            return false;
        }
    }
}
