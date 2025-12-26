using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._War.Exhaustion;

/// <summary>
/// Handles exhaustion mechanics for entities that can experience hunger.
/// Accumulates exhaustion damage when an entity is starving (at HungerThreshold.Dead).
/// </summary>
public sealed class ExhaustionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private DamageTypePrototype _exhaustionPrototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        _exhaustionPrototype = _prototype.Index<DamageTypePrototype>("Exhaustion");
        SubscribeLocalEvent<ExhaustionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ExhaustionComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnMapInit(EntityUid uid, ExhaustionComponent component, MapInitEvent args)
    {
        component.CurrentExhaustion = FixedPoint2.Zero;
        component.NextUpdateTime = _timing.CurTime + component.UpdateRate;
    }

    private void OnUnpaused(EntityUid uid, ExhaustionComponent component, ref EntityUnpausedEvent args)
    {
        component.NextUpdateTime += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExhaustionComponent, HungerComponent>();
        while (query.MoveNext(out var uid, out var exhaustion, out var hunger))
        {
            if (_timing.CurTime < exhaustion.NextUpdateTime)
                continue;

            exhaustion.NextUpdateTime = _timing.CurTime + exhaustion.UpdateRate;

            ProcessExhaustion(uid, exhaustion, hunger);
        }
    }

    private void ProcessExhaustion(EntityUid uid, ExhaustionComponent exhaustion, HungerComponent hunger)
    {
        var threshold = _hunger.GetHungerThreshold(hunger);

        // Process starvation exhaustion
        if (threshold == HungerThreshold.Dead)
        {
            var oldExhaustion = exhaustion.CurrentExhaustion;
            exhaustion.CurrentExhaustion += exhaustion.AccumulationRate;
            exhaustion.CurrentExhaustion = FixedPoint2.Min(exhaustion.CurrentExhaustion, exhaustion.MaxExhaustion);

            if (exhaustion.CurrentExhaustion > oldExhaustion)
            {
                var damageAmount = exhaustion.CurrentExhaustion - oldExhaustion;
                var damage = new DamageSpecifier(_exhaustionPrototype, damageAmount);
                _damageable.TryChangeDamage(uid, damage, true);
            }
            return;
        }

        // Process exhaustion healing
        if (exhaustion.CurrentExhaustion <= 0)
            return;

        var previousExhaustion = exhaustion.CurrentExhaustion;
        exhaustion.CurrentExhaustion -= exhaustion.HealRate;
        exhaustion.CurrentExhaustion = FixedPoint2.Max(exhaustion.CurrentExhaustion, FixedPoint2.Zero);

        if (exhaustion.CurrentExhaustion < previousExhaustion)
        {
            var toHeal = previousExhaustion - exhaustion.CurrentExhaustion;
            var heal = new DamageSpecifier(_exhaustionPrototype, -toHeal);
            _damageable.TryChangeDamage(uid, heal, true);
        }
    }

    /// <summary>
    /// Modifies the current exhaustion value of an entity.
    /// Used by medical items and other systems to heal or apply exhaustion.
    /// </summary>
    public void ModifyExhaustion(EntityUid uid, FixedPoint2 amount, ExhaustionComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var oldExhaustion = component.CurrentExhaustion;
        component.CurrentExhaustion = FixedPoint2.Clamp(
            component.CurrentExhaustion + amount,
            FixedPoint2.Zero,
            component.MaxExhaustion);

        if (component.CurrentExhaustion >= oldExhaustion)
            return;

        var toHeal = oldExhaustion - component.CurrentExhaustion;
        var heal = new DamageSpecifier(_exhaustionPrototype, -toHeal);
        _damageable.TryChangeDamage(uid, heal, true);
    }
}
