using Content.Server.Popups;
using Content.Shared._White.Blocking;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.Melee.Crit;

public sealed class CritSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CritComponent, MeleeHitEvent>(HandleHit, before: new [] {typeof(MeleeBlockSystem)});
    }

    private void HandleHit(EntityUid uid, CritComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0
            || !IsCriticalHit(component))
        {
            return;
        }

        var damage = args.BaseDamage.GetTotal() * component.CritMultiplier;

        args.BonusDamage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"),
            damage - args.BaseDamage.GetTotal());

        var message = Loc.GetString("melee-crit-damage", ("damage", damage));
        _popup.PopupEntity(message, args.User, args.User, PopupType.MediumCaution);
    }

    private bool IsCriticalHit(CritComponent component)
    {
        component.RealChance ??= component.CritChance;

        var isCritical = _random.NextFloat() <= component.RealChance;

        if (isCritical)
            component.RealChance = component.CritChance;
        else
            component.RealChance++;

        return isCritical;
    }
}
