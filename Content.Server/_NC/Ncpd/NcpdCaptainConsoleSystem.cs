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
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using System.Linq;

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

    // Каталог товаров
    private readonly Dictionary<string, (int Price, string Prototype)> _catalog = new()
    {
        { "ammo_ap", (800, "BoxMagazineRifleArmorPiercing") },
        { "emp_grenades", (1200, "BoxGrenadeEmp") },
        { "heavy_armor", (5000, "ClothingOuterArmorHeavy") }, // Условные названия прототипов
        { "hunter_bot", (10000, "MobCleanBot") } // Для примера
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdPurchaseMessage>(OnPurchase);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdRevokeAccessMessage>(OnRevokeAccess);
        SubscribeLocalEvent<NcpdCaptainConsoleComponent, NcpdClearLogsMessage>(OnClearLogs);
    }

    private void OnUiOpened(EntityUid uid, NcpdCaptainConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid);
    }

    private void UpdateUiState(EntityUid consoleUid)
    {
        var stationUid = _stationSystem.GetOwningStation(consoleUid);
        if (stationUid == null && _stationSystem.GetStationsSet().Count > 0)
        {
            stationUid = _stationSystem.GetStationsSet().First();
        }

        if (stationUid == null) return;

        // 1. Бюджет
        int budget = 0;
        var stationBank = _bankSystem.EnsureStationBank(stationUid.Value);
        if (stationBank.Accounts.TryGetValue(SectorBankAccount.Ncpd, out var account))
        {
            budget = account.Balance;
        }

        // 2. Логи
        var logs = new List<NcpdLogEntry>();
        if (TryComp<NcpdStationComponent>(stationUid.Value, out var ncpdComp))
        {
            logs = ncpdComp.Logs.ToList();
        }

        // 3. Персонал
        var personnel = new List<NcpdPersonnelData>();
        var mindQuery = EntityQueryEnumerator<MindContainerComponent>();
        while (mindQuery.MoveNext(out var playerUid, out var mindContainer))
        {
            if (!_mindSystem.TryGetMind(playerUid, out var mindId, out var mind, mindContainer))
                continue;

            if (!_jobSystem.MindTryGetJobId(mindId, out var jobId))
                continue;

            // Фильтруем только копов
            if (jobId?.Id is "SecurityOfficer" or "Warden" or "Detective" or "HoS")
            {
                bool isSuspended = _ncpdSystem.IsSuspended(stationUid.Value, playerUid);
                personnel.Add(new NcpdPersonnelData
                {
                    PlayerEntity = _entityManager.GetNetEntity(playerUid),
                    Name = Name(playerUid),
                    Job = jobId?.Id ?? "Unknown",
                    IsSuspended = isSuspended
                });
            }
        }

        var state = new NcpdCaptainConsoleBuiState(budget, logs, personnel);
        _uiSystem.SetUiState(consoleUid, NcpdCaptainConsoleUiKey.Key, state);
    }

    private void OnPurchase(EntityUid uid, NcpdCaptainConsoleComponent component, NcpdPurchaseMessage args)
    {
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null) return;

        if (!_catalog.TryGetValue(args.ItemId, out var product)) return;

        if (_bankSystem.TryFactionWithdraw(stationUid.Value, SectorBankAccount.Ncpd, product.Price))
        {
            // Спавним ящик рядом с консолью (в идеале на Cargo Pad)
            Spawn(product.Prototype, Transform(uid).Coordinates);
            UpdateUiState(uid);
        }
    }

    private void OnRevokeAccess(EntityUid uid, NcpdCaptainConsoleComponent component, NcpdRevokeAccessMessage args)
    {
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null) return;

        var targetUid = _entityManager.GetEntity(args.TargetEntity);
        if (!targetUid.Valid) return;

        // 1. Отстраняем в системе (чтобы не мог выписывать штрафы)
        _ncpdSystem.SetSuspended(stationUid.Value, targetUid, true);

        // 2. Лишаем доступов на ID-карте
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
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null) return;

        if (TryComp<NcpdStationComponent>(stationUid.Value, out var ncpdComp))
        {
            ncpdComp.Logs.Clear();
            Dirty(stationUid.Value, ncpdComp);
        }

        UpdateUiState(uid);
    }
}

