using Content.Server.Atmos.Components;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Robust.Shared.Timing;

namespace Content.Server._War.StationEngine;

public sealed class StationEngineSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationEngineComponent, DamageChangedEvent>(StationEngineDamageChanged);
        SubscribeLocalEvent<StationEngineComponent, BreakageEventArgs>(StationEngineBreakage);
    }


    // Отслеживание состояния и запуск таймера
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationEngineComponent>();
        while (query.MoveNext(out var uid, out var engine))
        {
            if (engine.EngineBroken && engine.ExplosionTimer < _timing.CurTime)
            {
                OnStationEngineDestruction(uid, engine);
            }
        }
    }

    // При ивенте изменения урона проверка был ли урон увеличен. Если нет - считается за ремонт и обнуляет таймер
    public void StationEngineDamageChanged(EntityUid uid, StationEngineComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
        {
            component.EngineBroken = false;

            var stationUid = _station.GetStationInMap(Transform(uid).MapID);
            var message = Loc.GetString("station-engine-repaired-message");
            var sender = Loc.GetString("station-engine-message-sender");
            _chatSystem.DispatchStationAnnouncement(stationUid ?? uid, message, sender, true, null, Color.Green);
        }
    }

    // При смене состояния компонента Destructible меняется статус и даётся оповещение
    public void StationEngineBreakage(EntityUid uid, StationEngineComponent component, BreakageEventArgs args)
    {
        component.EngineBroken = true;

        component.ExplosionTimer = _timing.CurTime + component.ExplosionInterval;

        var stationUid = _station.GetStationInMap(Transform(uid).MapID);
        var message = Loc.GetString("station-engine-broken-message",
            ("time", component.ExplosionInterval.Seconds));
        var sender = Loc.GetString("station-engine-message-sender");
        _chatSystem.DispatchStationAnnouncement(stationUid ?? uid, message, sender, true, null, Color.OrangeRed);
    }

    // метод обрабатывающий событие уничтожения двигателя
    private void OnStationEngineDestruction(EntityUid uid, StationEngineComponent engine)
    {
        var parent = Transform(uid).ParentUid; // берём айди сетки на которой стоит двигатель

        if (!HasComp<GridAtmosphereComponent>(parent)) // проверяем является ли сетка станцией по компоненту атмоса
            return;

        var eventArgs = new DestructionStationEngineEvent(parent);
        RaiseLocalEvent(uid, eventArgs);

        _explosion.QueueExplosion(uid, ExplosionSystem.DefaultExplosionPrototypeId, 800000, 9, 110); // операции взрыва ставятся в очередь для меньшей нагрузки
    }
}

// Объявление ивента взрыва
public sealed class DestructionStationEngineEvent : EntityEventArgs
{
    public readonly EntityUid Parent;

    public DestructionStationEngineEvent(EntityUid parent)
    {
        Parent = parent;
    }
}
