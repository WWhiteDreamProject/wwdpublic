using Content.Shared._NC.Trauma;
using Content.Shared._NC.Trauma.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Pinpointer;
using Robust.Shared.Player;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Audio;

namespace Content.Server._NC.Trauma
{
    public sealed class TraumaTabletSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedPinpointerSystem _pinpointer = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TraumaTabletComponent, BoundUIOpenedEvent>(OnTabletUiOpen);
            SubscribeLocalEvent<TraumaTabletComponent, TraumaOpenMapMsg>(OnOpenMap);
            // Обработка нажатия "Выполнено" на планшете
            SubscribeLocalEvent<TraumaTabletComponent, TraumaCompleteMissionMsg>(OnCompleteMission);

            // Подписываемся на события изменения здоровья для пациентов
            SubscribeLocalEvent<TraumaSubscriberComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<TraumaSubscriberComponent, DamageChangedEvent>(OnDamageChanged);
        }

        private void OnMobStateChanged(EntityUid uid, TraumaSubscriberComponent comp, MobStateChangedEvent args)
        {
            UpdateTabletsForPatient(uid);
        }

        private void OnDamageChanged(EntityUid uid, TraumaSubscriberComponent comp, DamageChangedEvent args)
        {
            UpdateTabletsForPatient(uid);
        }

        /// <summary>
        /// Диспетчер отправляет команду на ОДИН свободный планшет.
        /// Возвращает true, если свободный планшет найден.
        /// </summary>
        public bool DispatchTeam(EntityUid targetPatient)
        {
            var query = EntityQueryEnumerator<TraumaTabletComponent>();
            while (query.MoveNext(out var tabletUid, out var tabletComp))
            {
                // Защита: не отправлять на занятый планшет
                if (tabletComp.ActivePatient != null)
                    continue;

                // Звуковое оповещение и печать для планшета
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/alert.ogg"), tabletUid);
                _popup.PopupEntity($"!!! PLATINUM TRAUMA ALERT !!!\nPatient: {Name(targetPatient)}", tabletUid, PopupType.LargeCaution);

                tabletComp.ActivePatient = GetNetEntity(targetPatient);
                tabletComp.IsPendingCompletion = false; // Новая миссия — не pending
                Dirty(tabletUid, tabletComp);

                // Включаем встроенный компас (пинпоинтер) на цель
                var pinpointer = EnsureComp<PinpointerComponent>(tabletUid);
                _pinpointer.SetActive(tabletUid, true, pinpointer);
                _pinpointer.SetTarget(tabletUid, targetPatient, pinpointer);

                UpdateTabletInterface(tabletUid, tabletComp);

                // Отправляем только на ОДИН свободный планшет
                return true;
            }

            // Нет свободных планшетов
            return false;
        }

        /// <summary>
        /// Оперативник нажал "Выполнено" — помечаем pending на всех консолях.
        /// </summary>
        private void OnCompleteMission(EntityUid uid, TraumaTabletComponent component, TraumaCompleteMissionMsg args)
        {
            if (component.ActivePatient == null)
                return;

            var patientNetEntity = component.ActivePatient.Value;
            component.IsPendingCompletion = true;

            // Помечаем этого пациента как pending на всех консолях
            var consoleQuery = EntityQueryEnumerator<TraumaComputerComponent>();
            while (consoleQuery.MoveNext(out var consoleUid, out var consoleComp))
            {
                consoleComp.PendingCompletions.Add(patientNetEntity);
            }

            // Обновляем UI планшета (покажет "ожидание подтверждения")
            UpdateTabletInterface(uid, component);

            // Обновляем UI всех консолей
            var computerSystem = EntityManager.System<TraumaComputerSystem>();
            var consoles = EntityQueryEnumerator<TraumaComputerComponent>();
            while (consoles.MoveNext(out var cUid, out var cComp))
            {
                computerSystem.UpdateUserInterface(cUid, cComp);
            }
        }

        /// <summary>
        /// Вызывается после подтверждения диспетчером — очищает планшеты от этого пациента.
        /// </summary>
        public void ClearTabletMission(NetEntity patientNetEntity)
        {
            var query = EntityQueryEnumerator<TraumaTabletComponent>();
            while (query.MoveNext(out var tabletUid, out var tabletComp))
            {
                if (tabletComp.ActivePatient != patientNetEntity)
                    continue;

                tabletComp.ActivePatient = null;
                tabletComp.IsPendingCompletion = false;
                Dirty(tabletUid, tabletComp);

                // Отключаем пинпоинтер
                if (TryComp<PinpointerComponent>(tabletUid, out var pinpointer))
                {
                    _pinpointer.SetActive(tabletUid, false, pinpointer);
                    _pinpointer.SetTarget(tabletUid, null, pinpointer);
                }

                UpdateTabletInterface(tabletUid, tabletComp);
            }
        }

        private void UpdateTabletsForPatient(EntityUid patientUid)
        {
            var netPatient = GetNetEntity(patientUid);
            var query = EntityQueryEnumerator<TraumaTabletComponent>();
            while (query.MoveNext(out var tabletUid, out var tabletComp))
            {
                // Если планшет привязан к этому пациенту - обновляем UI
                if (tabletComp.ActivePatient == netPatient)
                {
                    UpdateTabletInterface(tabletUid, tabletComp);
                }
            }
        }

        private void OnTabletUiOpen(EntityUid uid, TraumaTabletComponent component, BoundUIOpenedEvent args)
        {
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

        public void UpdateTabletInterface(EntityUid tabletUid, TraumaTabletComponent component)
        {
            TraumaPatientData? patientData = null;

            if (component.ActivePatient != null)
            {
                var patientUid = GetEntity(component.ActivePatient.Value);
                if (TryComp<TraumaSubscriberComponent>(patientUid, out var sub) &&
                    TryComp<MobStateComponent>(patientUid, out var mobState))
                {
                    var meta = MetaData(patientUid);
                    string status = _mobState.IsDead(patientUid, mobState) ? "Dead" :
                                    (_mobState.IsCritical(patientUid, mobState) ? "Critical" : "Alive");

                    var damageInfo = "HP: 100%";
                    float brute = 0f;
                    float burn = 0f;
                    float toxin = 0f;

                    if (TryComp<DamageableComponent>(patientUid, out var damageable))
                    {
                        damageInfo = $"Dmg: {damageable.TotalDamage}";
                        if (damageable.DamagePerGroup.TryGetValue("Brute", out var bruteVal)) brute = bruteVal.Float();
                        if (damageable.DamagePerGroup.TryGetValue("Burn", out var burnVal)) burn = burnVal.Float();
                        if (damageable.DamagePerGroup.TryGetValue("Toxin", out var toxinVal)) toxin = toxinVal.Float();
                    }

                    var xform = Transform(patientUid);
                    var targetCoords = GetNetCoordinates(xform.Coordinates);

                    var jobTitle = "Unknown";
                    if (TryComp<MindContainerComponent>(patientUid, out var mindContainer) &&
                        mindContainer.Mind.HasValue &&
                        _jobs.MindTryGetJob(mindContainer.Mind.Value, out var prototype))
                    {
                        jobTitle = prototype.LocalizedName;
                    }

                    patientData = new TraumaPatientData
                    {
                        EntityUid = component.ActivePatient.Value,
                        Name = meta.EntityName,
                        HealthStatus = status,
                        Subscription = sub.Tier,
                        Job = jobTitle,
                        DamageInfo = damageInfo,
                        BruteDamage = brute,
                        BurnDamage = burn,
                        ToxinDamage = toxin,
                        TargetCoords = targetCoords
                    };
                }
            }

            // Шлем информацию только если интерфейс планшета сейчас активен
            if (_ui.HasUi(tabletUid, TraumaTabletUiKey.Key) && _ui.IsUiOpen(tabletUid, TraumaTabletUiKey.Key))
            {
                _ui.SetUiState(tabletUid, TraumaTabletUiKey.Key,
                    new TraumaTabletState(patientData, component.IsPendingCompletion));
            }
        }
    }
}

