using System.Linq;
using Content.Server.Damage.Components;
using Content.Shared._White.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Damage.Systems;

public sealed class DamageOnHitSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    private readonly Random _random = new Random();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHitComponent, MeleeHitEvent>(DamageSelf);
    }

    // Looks for a hit, then damages the entity an appropriate amount.
    private void DamageSelf(EntityUid uid, DamageOnHitComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Any()) {
            _damageableSystem.TryChangeDamage(uid, component.Damage, component.IgnoreResistances);
        }
    }
}
