using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared._NC.Bank;
using Content.Shared._NC.Bank.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Robust.Shared.Player;
using Content.Shared.GameTicking;
using Content.Server.Chat.Managers;
using Robust.Shared.Enums;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Shared.Roles.Jobs;
using Content.Shared.Mind; // Необходим для работы с MindSystem
using Content.Server.Popups;
using Robust.Shared.Localization;

namespace Content.Server._NC.Bank
{
    /// <summary>
    /// Основная система экономики. Отвечает за БД, транзакции и автоматическую зарплату.
    /// </summary>
    public sealed class BankSystem : EntitySystem
    {
        // === ЗАВИСИМОСТИ ===
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly SharedJobSystem _jobSystem = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!; // Добавлено для получения MindId
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        private ISawmill _log = default!;

        // === НАСТРОЙКИ ТАЙМЕРА ===
        // Интервал зарплаты (30 минут = 1800 секунд).
        private const float PaydayInterval = 1800.0f;
        private float _paydayTimer = 0.0f;


        // ==========================================
        //      РАБОТА СО СЧЕТАМИ ФРАКЦИЙ (StationBank)
        // ==========================================

        public override void Initialize()
        {
            base.Initialize();
            _log = Logger.GetSawmill("bank");

            SubscribeLocalEvent<StationBankComponent, MapInitEvent>(OnStationBankInit);
        }

        private void OnStationBankInit(EntityUid uid, StationBankComponent component, MapInitEvent args)
        {
            EnsureDefaultAccounts(component);
        }

        public StationBankComponent EnsureStationBank(EntityUid stationUid)
        {
            var bank = EnsureComp<StationBankComponent>(stationUid);
            EnsureDefaultAccounts(bank);
            return bank;
        }

        private void EnsureDefaultAccounts(StationBankComponent component)
        {
            EnsureAccount(component, SectorBankAccount.CityAdmin, 0, 0);
            EnsureAccount(component, SectorBankAccount.TraumaTeam, 10000, 5);
            EnsureAccount(component, SectorBankAccount.Militech, 25000, 8);
            EnsureAccount(component, SectorBankAccount.Biotechnica, 15000, 6);
            EnsureAccount(component, SectorBankAccount.Ncpd, 5000, 0);
        }
        private void EnsureAccount(StationBankComponent component, SectorBankAccount account, int defaultBalance, int defaultIncrease)
        {
            if (!component.Accounts.ContainsKey(account))
            {
                component.Accounts[account] = new StationBankAccountInfo
                {
                    Balance = defaultBalance,
                    IncreasePerSecond = defaultIncrease
                };
            }
        }
        /// <summary>
        /// Выполняется каждый тик. Отсчитывает время до зарплаты и начисляет доход фракциям.
        /// </summary>
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // 1. Зарплата игрокам
            _paydayTimer += frameTime;
            if (_paydayTimer >= PaydayInterval)
            {
                _paydayTimer -= PaydayInterval;
                ProcessPayday();
            }

            // 2. Пассивный доход фракций
            var query = EntityQueryEnumerator<StationBankComponent>();
            while (query.MoveNext(out var uid, out var bank))
            {
                bank.SecondsSinceLastIncrease += frameTime;
                if (bank.SecondsSinceLastIncrease >= 1.0f)
                {
                    bank.SecondsSinceLastIncrease -= 1.0f;

                    bool changed = false;
                    foreach (var account in bank.Accounts.Values)
                    {
                        if (account.IncreasePerSecond != 0)
                        {
                            account.Balance += account.IncreasePerSecond;
                            changed = true;
                        }
                    }

                    if (changed)
                        Dirty(uid, bank);
                }
            }
        }

        /// <summary>
        /// Попытка списать средства со счета фракции.
        /// </summary>
        public bool TryFactionWithdraw(EntityUid stationUid, SectorBankAccount accountType, int amount)
        {
            if (amount <= 0) return false;

            var bank = EnsureStationBank(stationUid);
            if (!bank.Accounts.TryGetValue(accountType, out var account)) return false;

            if (account.Balance < amount) return false;

            account.Balance -= amount;
            Dirty(stationUid, bank);
            return true;
        }

        /// <summary>
        /// Попытка зачислить средства на счет фракции.
        /// </summary>
        public bool TryFactionDeposit(EntityUid stationUid, SectorBankAccount accountType, int amount)
        {
            if (amount <= 0) return false;

            var bank = EnsureStationBank(stationUid);

            if (!bank.Accounts.TryGetValue(accountType, out var account)) return false;

            account.Balance += amount;
            Dirty(stationUid, bank);
            return true;
        }

        private void ProcessPayday()
        {
            _log.Info("PAYDAY: Начало начисления зарплат...");
            int count = 0;

            foreach (var session in _playerManager.Sessions)
            {
                if (session.Status != SessionStatus.InGame || session.AttachedEntity is not { Valid: true } playerUid)
                    continue;

                int salary = GetSalaryForPlayer(playerUid);

                if (TryBankDeposit(playerUid, salary))
                {
                    count++;

                    _popupSystem.PopupEntity(Loc.GetString("bank-payday-message", ("amount", salary)), playerUid, playerUid);
                }
            }

            _log.Info($"PAYDAY: Зарплата выдана {count} игрокам.");
        }

        /// <summary>
        /// Определяет сумму зарплаты, читая поле 'salary' из прототипа работы.
        /// </summary>
        private int GetSalaryForPlayer(EntityUid uid)
        {
            // 1. Получаем MindId
            if (!_mindSystem.TryGetMind(uid, out var mindId, out _))
                return 50;

            // 2. Получаем Прототип работы напрямую
            if (_jobSystem.MindTryGetJob(mindId, out var jobProto))
            {
                return jobProto.Salary;
            }

            return 50; // Дефолт, если работа не найдена
        }

        // ==========================================
        //      РАБОТА С БАЗОЙ ДАННЫХ (Ваш код)
        // ==========================================

        public int GetBalance(EntityUid mobUid)
        {
            if (!_playerManager.TryGetSessionByEntity(mobUid, out var session)) return 0;

            var prefs = _prefsManager.GetPreferences(session.UserId);
            if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile) return 0;

            return profile.BankBalance;
        }

        public bool TryBankWithdraw(EntityUid mobUid, int amount)
        {
            if (amount <= 0) return false;
            return ModifyBalance(mobUid, -amount);
        }

        public bool TryBankDeposit(EntityUid mobUid, int amount)
        {
            if (amount <= 0) return false;
            return ModifyBalance(mobUid, amount);
        }

        /// <summary>
        /// Изменяет баланс и сохраняет профиль в БД.
        /// </summary>
        private bool ModifyBalance(EntityUid mobUid, int delta)
        {
            // 1. Получаем сессию
            if (!_playerManager.TryGetSessionByEntity(mobUid, out var session))
            {
                _log.Error($"Session not found for entity {mobUid}");
                return false;
            }

            // 2. Получаем настройки (Preferences)
            var prefs = _prefsManager.GetPreferences(session.UserId);
            if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
            {
                _log.Error($"Profile is not Humanoid for user {session.Name}");
                return false;
            }

            // 3. Проверка на минус
            if (delta < 0 && profile.BankBalance < -delta)
            {
                return false; // Недостаточно средств
            }

            // 4. Вычисляем новый баланс
            var newBalance = profile.BankBalance + delta;

            // 5. Создаем новый профиль с обновленным балансом
            var newProfile = profile.WithBankBalance(newBalance);

            // 6. СОХРАНЯЕМ В БД
            // Используем SelectedCharacterIndex для надежности
            _prefsManager.SetProfile(session.UserId, prefs.SelectedCharacterIndex, newProfile);

            _log.Info($"User {session.Name} balance updated: {profile.BankBalance} -> {newBalance}. Saved to DB.");
            return true;
        }
    }
}





