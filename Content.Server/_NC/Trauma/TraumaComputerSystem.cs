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
            SubscribeLocalEvent<TraumaComputerComponent, BoundUIOpenedEvent>(OnUiOpen);
            SubscribeLocalEvent<TraumaComputerComponent, InteractUsingEvent>(OnInteractUsing);

            // Tablet Events
            SubscribeLocalEvent<TraumaTabletComponent, BoundUIOpenedEvent>(OnTabletUiOpen);
            SubscribeLocalEvent<TraumaTabletComponent, TraumaOpenMapMsg>(OnOpenMap);
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
                UpdateUserInterface(uid, comp.Logs, TraumaComputerUiKey.Key);
            }

            // Update all tablets
            var tabletQuery = EntityQueryEnumerator<TraumaTabletComponent>();
            while (tabletQuery.MoveNext(out var uid, out var comp))
            {
                if (comp.ActivePatient != null)
                {
                    UpdateTabletInterface(uid, comp);
                }
            }
        }

        // --- CONSOLE HANDLERS ---

        private void OnUiOpen(EntityUid uid, TraumaComputerComponent component, BoundUIOpenedEvent args)
        {
            UpdateUserInterface(uid, component.Logs, TraumaComputerUiKey.Key);
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
                    UpdateUserInterface(uid, component.Logs, TraumaComputerUiKey.Key);
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

                UpdateUserInterface(uid, component.Logs, TraumaComputerUiKey.Key);
            }
        }

        private void OnDispatch(EntityUid uid, TraumaComputerComponent component, TraumaDispatchMsg args)
        {
            var target = GetEntity(args.TargetEntity);
            DispatchTeam(target, uid);
        }

        // --- TABLET HANDLERS ---

        private void OnTabletUiOpen(EntityUid uid, TraumaTabletComponent component, BoundUIOpenedEvent args)
        {
            // Send current active patient if any
            UpdateTabletInterface(uid, component);
        }

        private void OnOpenMap(EntityUid uid, TraumaTabletComponent component, TraumaOpenMapMsg args)
        {
            var user = args.Actor;
            if (_ui.HasUi(uid, StationMapUiKey.Key))
            {
                _ui.OpenUi(uid, StationMapUiKey.Key, user);
            }
        }

        // --- HELPERS ---

        private void DispatchTeam(EntityUid targetPatient, EntityUid sourceConsole)
        {
            // Update ALL tablets to point to this patient
            var query = EntityQueryEnumerator<TraumaTabletComponent, TransformComponent>();
            while (query.MoveNext(out var tabletUid, out var tabletComp, out var tabletXform))
            {
                // Play sound
                _audio.PlayPvs("/Audio/Effects/alert.ogg", tabletUid);

                // Popup
                _popup.PopupEntity($"!!! PLATINUM TRAUMA ALERT !!!\nPatient: {Name(targetPatient)}", tabletUid, PopupType.LargeCaution);

                // Set Active Patient
                tabletComp.ActivePatient = GetNetEntity(targetPatient);
                Dirty(tabletUid, tabletComp);

                // Update UI for this tablet
                UpdateTabletInterface(tabletUid, tabletComp);
            }
        }

        private void UpdateTabletInterface(EntityUid tabletUid, TraumaTabletComponent component)
        {
            TraumaPatientData? patientData = null;

            if (component.ActivePatient != null)
            {
                _sawmill.Info($"Updating tablet {tabletUid} for patient {component.ActivePatient}");

                var patientUid = GetEntity(component.ActivePatient.Value);
                // Fetch data for this specific patient
                if (TryComp<TraumaSubscriberComponent>(patientUid, out var sub) &&
                    TryComp<MetaDataComponent>(patientUid, out var meta) &&
                    TryComp<MobStateComponent>(patientUid, out var mobState) &&
                    TryComp<MindContainerComponent>(patientUid, out var mindContainer))
                {
                    string status = _mobState.IsDead(patientUid, mobState) ? "Dead" :
                                    (_mobState.IsCritical(patientUid, mobState) ? "Critical" : "Alive");

                    var damageInfo = "HP: 100%";
                    if (TryComp<DamageableComponent>(patientUid, out var damageable))
                        damageInfo = $"Dmg: {damageable.TotalDamage}";

                    var jobTitle = "Unknown";
                    if (mindContainer.Mind.HasValue && _jobs.MindTryGetJob(mindContainer.Mind.Value, out var prototype))
                        jobTitle = prototype.LocalizedName;

                    patientData = new TraumaPatientData
                    {
                        EntityUid = component.ActivePatient.Value,
                        Name = meta.EntityName,
                        HealthStatus = status,
                        Subscription = sub.Tier,
                        Job = jobTitle,
                        DamageInfo = damageInfo
                    };
                }
                else
                {
                    // Patient invalid
                    _sawmill.Info($"Tablet {tabletUid}: Patient entity invalid or missing components");
                }
            }

            _ui.SetUiState(tabletUid, TraumaTabletUiKey.Key, new TraumaTabletState(patientData));
        }

        private void UpdateUserInterface(EntityUid uid, List<TraumaLogEntry> logs, Enum uiKey)
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

            _ui.SetUiState(uid, uiKey, new TraumaComputerState(patients, logs));
        }

        public void UpdateUserInterface(EntityUid uid, TraumaComputerComponent? component = null)
        {
            UpdateUserInterface(uid, component?.Logs ?? new List<TraumaLogEntry>(), TraumaComputerUiKey.Key);
        }
    }
}
