using Content.Shared._NC.Trauma;
using Content.Shared._NC.Trauma.Components;
using Content.Shared.Mobs; // Нужно для Enum MobState (если понадобится)
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems; // Здесь живут IsDead, IsCritical
using Robust.Shared.Player;
using Robust.Server.GameObjects;
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

namespace Content.Server._NC.Trauma
{
    public sealed class TraumaComputerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly Robust.Shared.Timing.IGameTiming _timing = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TraumaComputerComponent, TraumaChangeSubscriptionMsg>(OnSubscriptionChange);
            SubscribeLocalEvent<TraumaComputerComponent, BoundUIOpenedEvent>(OnUiOpen);
            SubscribeLocalEvent<TraumaComputerComponent, InteractUsingEvent>(OnInteractUsing);
        }



        private void UpdateUserInterface(EntityUid uid, TraumaComputerComponent? component = null)
        {
            if (!Resolve(uid, ref component)) return;

            var patients = new List<TraumaPatientData>();

            // Ищем игроков (MindContainer - есть разум, даже если SSD)
            var query = EntityQueryEnumerator<TraumaSubscriberComponent, MetaDataComponent, MobStateComponent, MindContainerComponent>();

            while (query.MoveNext(out var entity, out var sub, out var meta, out var mobState, out var mindContainer))
            {

                string status = "Unknown";

                if (_mobState.IsDead(entity, mobState))
                {
                    status = "Dead";
                }
                else if (_mobState.IsCritical(entity, mobState))
                {
                    status = "Critical";
                }
                else
                {
                    status = "Alive"; // Или "Healthy"
                }

                // Get Damage
                var damageInfo = "HP: 100%";
                if (TryComp<DamageableComponent>(entity, out var damageable))
                {
                    damageInfo = $"Dmg: {damageable.TotalDamage}";
                }

                // Get Job
                var jobTitle = "Unknown";
                if (mindContainer.Mind.HasValue &&
                    _jobs.MindTryGetJob(mindContainer.Mind.Value, out var prototype))
                {
                    jobTitle = prototype.LocalizedName;
                }

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

            _ui.SetUiState(uid, TraumaComputerUiKey.Key, new TraumaComputerState(patients, component.Logs));
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
                    UpdateUserInterface(uid, component);
                }
                else
                {
                    _popup.PopupEntity($"Вы уже подписаны! Ваш статус: {subscriber.Tier}", uid, args.User);
                }
            }
        }

        private void OnUiOpen(EntityUid uid, TraumaComputerComponent component, BoundUIOpenedEvent args)
        {
            UpdateUserInterface(uid, component);
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
                    // Keep log size reasonable
                    if (component.Logs.Count > 50) component.Logs.RemoveAt(0);
                }

                UpdateUserInterface(uid, component);
            }
        }

        // Cleaned up duplicate method
    }
}
