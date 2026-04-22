using Content.Shared._White.Shove;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;

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
        if (!args.OtherFixture.Hard)
            return;

        var originalSpeed = component.OriginalSpeed;
        var effectiveSpeed = MathF.Max(originalSpeed, args.OurBody.LinearVelocity.Length());

        if (effectiveSpeed >= 20f)
        {
            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", 5 * (0.5f * effectiveSpeed / 20f));
            _damageable.TryChangeDamage(uid, damage);
        }

        RemComp<ShoveImpactComponent>(uid);
    }
}