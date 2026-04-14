using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware.Events;
using Content.Shared._NC.Trail;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._NC.Cyberware.Systems;

/// <summary>
///     Универсальная система для обработки боевых эффектов имплантов: броня, уклонение и т.д.
/// </summary>
public sealed class SharedCyberwareCombatSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    // Храним сущность и время, когда нужно удалить шлейф
    private readonly List<(EntityUid Uid, TimeSpan RemoveTime)> _activeTrails = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, CyberwareComponent component, DamageModifyEvent args)
    {
        // Только если есть источник урона (чтобы не уклоняться от голода, холода, радиации и прочего пассивного урона)
        bool isAttack = args.Origin != null && args.Origin != uid;

        if (isAttack)
        {
            // 1. Сначала обрабатываем уклонение (Керензиков)
            foreach (var implantUid in component.InstalledImplants.Values)
            {
                if (!_entManager.TryGetComponent<CyberwareDodgeComponent>(implantUid, out var dodge))
                    continue;

                if (_timing.CurTime < dodge.NextDodgeTime)
                    continue;

                if (!_random.Prob(dodge.Chance))
                    continue;

                // Уклонение сработало! Обнуляем урон.
                args.Damage *= 0;
                dodge.NextDodgeTime = _timing.CurTime + TimeSpan.FromSeconds(dodge.Cooldown);
                Dirty(implantUid, dodge);

                // Отправляем событие на клиент для отрисовки визуала, только если мы на сервере
                if (_netManager.IsServer)
                {
                    RaiseNetworkEvent(new CyberwareDodgeEvent(GetNetEntity(uid)));
                    
                    // Добавляем шлейф на сервере, чтобы он гарантированно синхронизировался всем клиентам
                    TriggerDodgeTrail(uid);
                }
                
                return; // Если уклонились, броню считать уже не нужно
            }
        }

        // 2. Если не уклонились или это не атака, применяем броню от имплантов (Подслои)
        foreach (var implantUid in component.InstalledImplants.Values)
        {
            if (!_entManager.TryGetComponent<ArmorComponent>(implantUid, out var armor))
                continue;

            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, armor.Modifiers);
        }
    }

    private void TriggerDodgeTrail(EntityUid uid)
    {
        var trail = EnsureComp<TrailComponent>(uid);
        trail.RenderedEntity = uid;
        trail.Color = Color.FromHex("#00FFFF").WithAlpha(0.5f);
        trail.Frequency = 0.03f;
        trail.Lifetime = 0.2f;
        trail.AlphaLerpAmount = 0.2f;
        
        // Удаляем шлейф ровно через 0.2 секунды
        _activeTrails.Add((uid, _timing.CurTime + TimeSpan.FromSeconds(0.2)));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_netManager.IsServer && _activeTrails.Count > 0)
        {
            var curTime = _timing.CurTime;
            for (int i = _activeTrails.Count - 1; i >= 0; i--)
            {
                var (uid, removeTime) = _activeTrails[i];
                if (curTime >= removeTime)
                {
                    if (Exists(uid))
                        RemComp<TrailComponent>(uid);
                    
                    _activeTrails.RemoveAt(i);
                }
            }
        }
    }
}
