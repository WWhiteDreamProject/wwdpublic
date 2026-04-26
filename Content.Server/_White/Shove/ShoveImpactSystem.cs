using Content.Shared._White.Shove;
using Content.Shared.Damage;
using Content.Shared.Physics;
using Robust.Shared.Physics.Events;

namespace Content.Server._White.Shove;

public sealed class ShoveImpactSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShoveImpactComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, ShoveImpactComponent component, ref StartCollideEvent args)
    {
        if ((args.OtherFixture.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
            return;

        var speed = args.OurBody.LinearVelocity.Length();

        if (speed >= 20f)
        {
            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", 5 * (speed / 20f));
            _damageable.TryChangeDamage(uid, damage);
        }

        RemComp<ShoveImpactComponent>(uid);
    }
}
