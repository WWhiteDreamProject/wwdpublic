using Content.Shared._NC.Trauma;
using Content.Shared._NC.Trauma.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using Content.Shared.Mind.Components;
using Content.Shared.Interaction;
using Content.Shared.Access.Components;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Mind;
using Content.Server.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Audio.Systems;
using Content.Shared.Pinpointer;
using Robust.Shared.Log;

namespace Content.Server._NC.Trauma
{
    public sealed class TraumaComputerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly Robust.Shared.Timing.IGameTiming _timing = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        [Dependency] private readonly ILogManager _logManager = default!;
        private ISawmill _sawmill = default!;

        private float _updateTimer;
        private const float UpdateInterval = 1.0f; // Update every 1.0 second

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = _logManager.GetSawmill("trauma");

            // Console Events
            SubscribeLocalEvent<TraumaComputerComponent, TraumaChangeSubscriptionMsg>(OnSubscriptionChange);
            SubscribeLocalEvent<TraumaComputerComponent, TraumaDispatchMsg>(OnDispatch);
            SubscribeLocalEvent<TraumaComputerComponent, TraumaConfirmCompletionMsg>(OnConfirmCompletion);
            SubscribeLocalEvent<TraumaComputerComponent, BoundUIOpenedEvent>(OnUiOpen);
            SubscribeLocalEvent<TraumaComputerComponent, InteractUsingEvent>(OnInteractUsing);
        }

        public override void Update(float frameTime)
        {
            _updateTimer += frameTime;
            if (_updateTimer >= UpdateInterval)
            {
                _updateTimer -= UpdateInterval;
                UpdateAllInterfaces();
            }
        }

        private void UpdateAllInterfaces()
        {
            // Update all consoles
            var consoleQuery = EntityQueryEnumerator<TraumaComputerComponent>();
            while (consoleQuery.MoveNext(out var uid, out var comp))
            {
                UpdateUserInterface(uid, comp.Logs, TraumaComputerUiKey.Key, comp.PendingCompletions);
            }
        }

        // --- CONSOLE HANDLERS ---

        private void OnUiOpen(EntityUid uid, TraumaComputerComponent component, BoundUIOpenedEvent args)
        {
            UpdateUserInterface(uid, component.Logs, TraumaComputerUiKey.Key, component.PendingCompletions);
        }

        private void OnInteractUsing(EntityUid uid, TraumaComputerComponent component, InteractUsingEvent args)
        {
            if (TryComp<IdCardComponent>(args.Used, out var idCard))
            {
                var user = args.User;
                // Add subscription
                var subscriber = EnsureComp<TraumaSubscriberComponent>(user);

                // If they had None, set to Bronze
                if (subscriber.Tier == TraumaSubscriptionTier.None)
                {
                    subscriber.Tier = TraumaSubscriptionTier.Bronze;
                    Dirty(user, subscriber);
                    _popup.PopupEntity("Вы успешно подписались на Trauma Team (Bronze)!", uid, args.User);
                    UpdateUserInterface(uid, component.Logs, TraumaComputerUiKey.Key, component.PendingCompletions);
                }
                else
                {
                    _popup.PopupEntity($"Вы уже подписаны! Ваш статус: {subscriber.Tier}", uid, args.User);
                }
            }
        }

        private void OnSubscriptionChange(EntityUid uid, TraumaComputerComponent component, TraumaChangeSubscriptionMsg args)
        {
            var targetEntity = GetEntity(args.TargetEntity);

            if (TryComp<TraumaSubscriberComponent>(targetEntity, out var subscriber))
            {
                var oldTier = subscriber.Tier;
                subscriber.Tier = args.NewTier;
                Dirty(targetEntity, subscriber);

                // Logging
                if (args.Actor.Valid)
                {
                    var editorName = Name(args.Actor);
                    var targetName = Name(targetEntity);

                    var log = new TraumaLogEntry
                    {
                        Time = _timing.CurTime,
                        Editor = editorName,
                        Target = targetName,
                        OldTier = oldTier,
                        NewTier = args.NewTier
                    };

                    component.Logs.Add(log);
                    if (component.Logs.Count > 50) component.Logs.RemoveAt(0);
                }

                UpdateUserInterface(uid, component.Logs, TraumaComputerUiKey.Key, component.PendingCompletions);
            }
        }

        private void OnDispatch(EntityUid uid, TraumaComputerComponent component, TraumaDispatchMsg args)
        {
            var target = GetEntity(args.TargetEntity);
            if (TryComp<TraumaSubscriberComponent>(target, out var subscriber) && subscriber.Tier == TraumaSubscriptionTier.Platinum)
            {
                var tabletSystem = EntityManager.System<TraumaTabletSystem>();
                if (tabletSystem.DispatchTeam(target))
                {
                    _popup.PopupEntity("Команда успешно отправлена!", uid, args.Actor);
                }
                else
                {
                    _popup.PopupEntity("Ошибка: Нет свободных планшетов для отправки.", uid, args.Actor, PopupType.LargeCaution);
                }
            }
            else
            {
                _popup.PopupEntity("Ошибка: Цель должна иметь уровень страховки Platinum.", uid, args.Actor, PopupType.LargeCaution);
            }
        }

        /// <summary>
        /// Диспетчер подтвердил завершение миссии.
        /// </summary>
        private void OnConfirmCompletion(EntityUid uid, TraumaComputerComponent component, TraumaConfirmCompletionMsg args)
        {
            var patientNetEntity = args.TargetEntity;

            // Убираем из pending
            component.PendingCompletions.Remove(patientNetEntity);

            // Очищаем планшеты
            var tabletSystem = EntityManager.System<TraumaTabletSystem>();
            tabletSystem.ClearTabletMission(patientNetEntity);

            // Также убираем pending со всех остальных консолей
            var consoleQuery = EntityQueryEnumerator<TraumaComputerComponent>();
            while (consoleQuery.MoveNext(out var consoleUid, out var consoleComp))
            {
                consoleComp.PendingCompletions.Remove(patientNetEntity);
            }

            _popup.PopupEntity("Миссия подтверждена! Планшеты очищены.", uid, args.Actor);
            UpdateUserInterface(uid, component);
        }

        // --- HELPERS ---

        private void UpdateUserInterface(EntityUid uid, List<TraumaLogEntry> logs, Enum uiKey, HashSet<NetEntity>? pendingCompletions = null)
        {
            var patients = new List<TraumaPatientData>();

            var query = EntityQueryEnumerator<TraumaSubscriberComponent, MetaDataComponent, MobStateComponent, MindContainerComponent>();

            while (query.MoveNext(out var entity, out var sub, out var meta, out var mobState, out var mindContainer))
            {
                string status = _mobState.IsDead(entity, mobState) ? "Dead" :
                                (_mobState.IsCritical(entity, mobState) ? "Critical" : "Alive");

                var damageInfo = "HP: 100%";
                if (TryComp<DamageableComponent>(entity, out var damageable))
                    damageInfo = $"Dmg: {damageable.TotalDamage}";

                var jobTitle = "Unknown";
                if (mindContainer.Mind.HasValue && _jobs.MindTryGetJob(mindContainer.Mind.Value, out var prototype))
                    jobTitle = prototype.LocalizedName;

                patients.Add(new TraumaPatientData
                {
                    EntityUid = GetNetEntity(entity),
                    Name = meta.EntityName,
                    HealthStatus = status,
                    Subscription = sub.Tier,
                    Job = jobTitle,
                    DamageInfo = damageInfo
                });
            }

            _ui.SetUiState(uid, uiKey, new TraumaComputerState(patients, logs, pendingCompletions));
        }

        public void UpdateUserInterface(EntityUid uid, TraumaComputerComponent? component = null)
        {
            UpdateUserInterface(uid, component?.Logs ?? new List<TraumaLogEntry>(), TraumaComputerUiKey.Key, component?.PendingCompletions);
        }
    }
}
