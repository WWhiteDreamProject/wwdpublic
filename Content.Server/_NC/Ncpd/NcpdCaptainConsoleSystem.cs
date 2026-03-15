using Content.Shared._NC.Ncpd;
using Content.Server.Station.Systems;
using Content.Server._NC.Bank;
using Content.Shared._NC.Bank;
using Content.Shared._NC.Bank.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Stacks;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using System.Linq;
using System.Collections.Generic;

namespace Content.Server._NC.Ncpd;

public sealed class NcpdCaptainConsoleSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly BankSystem _bankSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly NcpdSystem _ncpdSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    private readonly Dictionary<string, (int Price, string Prototype)> _catalog = new()
    {
        { "ammo_ap", (800, "BoxMagazineRifleArmorPiercing") },
        { "emp_grenades", (1200, "BoxGrenadeEmp") },
        { "heavy_armor", (5000, "ClothingOuterArmorHeavy") }, 
        { "hunter_bot", (10000, "MobCleanBot") } 
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdPurchaseMessage>(OnPurchase);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdRevokeAccessMessage>(OnRevokeAccess);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdClearLogsMessage>(OnClearLogs);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdWithdrawBudgetMessage>(OnWithdrawBudget);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdDepositBudgetMessage>(OnDepositBudget);
        
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
    }

    private void OnUiOpened(EntityUid uid, NcpdCaptainConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid);
    }

    private void OnItemInserted(EntityUid uid, NcpdCaptainConsoleComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateUiState(uid);
    }

    private void OnItemRemoved(EntityUid uid, NcpdCaptainConsoleComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateUiState(uid);
    }

    private void UpdateUiState(EntityUid consoleUid)
    {
        EntityUid? stationUid = GetStation(consoleUid);
        if (stationUid == null) return;

        // 1. Бюджет
        int budget = 0;
        var stationBank = _bankSystem.EnsureStationBank(stationUid.Value);
        if (stationBank.Accounts.TryGetValue(SectorBankAccount.Ncpd, out var account))
        {
            budget = account.Balance;
        }

        // 2. Наличность в слоте
        int insertedCash = 0;
        if (_itemSlotsSystem.TryGetSlot(consoleUid, "cash_slot", out var slot) && slot.HasItem)
        {
            if (TryComp<StackComponent>(slot.Item, out var stack) && stack.StackTypeId == "Credit")
            {
                insertedCash = stack.Count;
            }
        }

        // 3. Логи
        var logs = new List<NcpdLogEntry>();
        if (TryComp<NcpdStationComponent>(stationUid.Value, out var ncpdComp))
        {
            logs = ncpdComp.Logs.ToList();
        }

        // 4. Персонал
        var personnel = new List<NcpdPersonnelData>();
        var mindQuery = EntityQueryEnumerator<MindContainerComponent>();
        while (mindQuery.MoveNext(out var playerUid, out var mindContainer))
        {
            if (!_mindSystem.TryGetMind(playerUid, out var mindId, out var mind, mindContainer)) continue;
            if (!_jobSystem.MindTryGetJobId(mindId, out var jobId)) continue;

            if (jobId?.Id is "SecurityOfficer" or "Warden" or "Detective" or "HoS" or "Captain")
            {
                bool isSuspended = _ncpdSystem.IsSuspended(stationUid.Value, playerUid);
                string jobTitle = jobId?.Id ?? "Unknown";
                if (_idCardSystem.TryFindIdCard(playerUid, out var idCard) && !string.IsNullOrWhiteSpace(idCard.Comp.LocalizedJobTitle))
                    jobTitle = idCard.Comp.LocalizedJobTitle;

                personnel.Add(new NcpdPersonnelData {
                    PlayerEntity = _entityManager.GetNetEntity(playerUid),
                    Name = Name(playerUid),
                    Job = jobTitle,
                    IsSuspended = isSuspended
                });
            }
        }

        var state = new NcpdCaptainConsoleBuiState(budget, insertedCash, logs, personnel);
        _uiSystem.SetUiState(consoleUid, NcpdCaptainConsoleUiKey.Key, state);
    }

    private void OnWithdrawBudget(EntityUid uid, NcpdCaptainConsoleComponent component, NcpdWithdrawBudgetMessage args)
    {
        EntityUid? stationUid = GetStation(uid);
        if (stationUid == null) return;

        if (_bankSystem.TryFactionWithdraw(stationUid.Value, SectorBankAccount.Ncpd, args.Amount))
        {
            var cash = Spawn("SpaceCash", Transform(uid).Coordinates);
            _stackSystem.SetCount(cash, args.Amount);
            AddFinancialLog(stationUid.Value, args.Actor, "СНЯТИЕ СРЕДСТВ", args.Amount);
            UpdateUiState(uid);
        }
    }

    private void OnDepositBudget(EntityUid uid, NcpdCaptainConsoleComponent component, NcpdDepositBudgetMessage args)
    {
        EntityUid? stationUid = GetStation(uid);
        if (stationUid == null) return;

        if (_itemSlotsSystem.TryGetSlot(uid, "cash_slot", out var slot) && slot.HasItem)
        {
            if (TryComp<StackComponent>(slot.Item, out var stack) && stack.StackTypeId == "Credit")
            {
                int available = stack.Count;
                int toDeposit = Math.Min(args.Amount, available);

                if (toDeposit > 0 && _bankSystem.TryFactionDeposit(stationUid.Value, SectorBankAccount.Ncpd, toDeposit))
                {
                    _stackSystem.SetCount(slot.Item.Value, available - toDeposit, stack);
                    AddFinancialLog(stationUid.Value, args.Actor, "ПОПОЛНЕНИЕ СЧЕТА", toDeposit);
                    
                    if (stack.Count <= 0)
                        _entityManager.DeleteEntity(slot.Item.Value);

                    UpdateUiState(uid);
                }
            }
        }
    }

    private void AddFinancialLog(EntityUid stationUid, EntityUid actor, string status, int amount)
    {
        if (!TryComp<NcpdStationComponent>(stationUid, out var ncpdComp)) return;

        var log = new NcpdLogEntry {
            Time = _timing.CurTime,
            OfficerName = Name(actor),
            TargetName = "ДЕПАРТАМЕНТ",
            Amount = amount,
            Status = status,
            Reason = string.Empty
        };

        ncpdComp.Logs.Add(log);
        if (ncpdComp.Logs.Count > 100) ncpdComp.Logs.RemoveAt(0);
        Dirty(stationUid, ncpdComp);
    }

    private void OnPurchase(EntityUid uid, NcpdCaptainConsoleComponent component, NcpdPurchaseMessage args)
    {
        EntityUid? stationUid = GetStation(uid);
        if (stationUid == null) return;
        if (!_catalog.TryGetValue(args.ItemId, out var product)) return;

        if (_bankSystem.TryFactionWithdraw(stationUid.Value, SectorBankAccount.Ncpd, product.Price))
        {
            Spawn(product.Prototype, Transform(uid).Coordinates);
            AddFinancialLog(stationUid.Value, args.Actor, $"ЗАКУПКА: {args.ItemId}", product.Price);
            UpdateUiState(uid);
        }
    }

    private void OnRevokeAccess(EntityUid uid, NcpdCaptainConsoleComponent component, NcpdRevokeAccessMessage args)
    {
        EntityUid? stationUid = GetStation(uid);
        if (stationUid == null) return;
        var targetUid = _entityManager.GetEntity(args.TargetEntity);
        if (!targetUid.Valid) return;

        _ncpdSystem.SetSuspended(stationUid.Value, targetUid, true);
        if (_idCardSystem.TryFindIdCard(targetUid, out var idCard))
        {
            if (TryComp<AccessComponent>(idCard.Owner, out var access))
            {
                access.Tags.Remove("Security");
                access.Tags.Remove("Armory");
                access.Tags.Remove("Brig");
                access.Tags.Remove("Command");
                Dirty(idCard.Owner, access);
            }
        }
        UpdateUiState(uid);
    }

    private void OnClearLogs(EntityUid uid, NcpdCaptainConsoleComponent component, NcpdClearLogsMessage args)
    {
        EntityUid? stationUid = GetStation(uid);
        if (stationUid == null) return;
        if (TryComp<NcpdStationComponent>(stationUid.Value, out var ncpdComp))
        {
            ncpdComp.Logs.Clear();
            Dirty(stationUid.Value, ncpdComp);
        }
        UpdateUiState(uid);
    }

    private EntityUid? GetStation(EntityUid console)
    {
        var station = _stationSystem.GetOwningStation(console);
        if (station == null) station = _stationSystem.GetStationsSet().FirstOrDefault();
        if (station == null)
        {
            var queryBank = EntityQueryEnumerator<StationBankComponent>();
            if (queryBank.MoveNext(out var bankUid, out _)) station = bankUid;
        }
        return station;
    }
}
