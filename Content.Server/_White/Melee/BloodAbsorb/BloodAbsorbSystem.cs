using Content.Server._White.Melee.Crit;
using Content.Server._White.Other.BloodLust;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.Melee.BloodAbsorb;

public sealed class BloodAbsorbSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodAbsorbComponent, CritHitEvent>(OnCritHit);
        SubscribeLocalEvent<BloodAbsorbComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnCritHit(EntityUid uid, BloodAbsorbComponent component, CritHitEvent args)
    {
        if(args.Targets.Count == 0
           || args.Targets[0] == args.User)
            return;

        var absorbed = _random.Next(component.MinAbsorb, component.MaxAbsorb);

        if (args.Targets.Count != 1) // Heavy attack
            absorbed = (int) MathF.Round(absorbed * 0.7f);

        foreach (var target in args.Targets)
        {
            if (!TryComp(target, out BloodstreamComponent? bloodstream))
                continue;

            var blood = bloodstream.BloodSolution;

            if (blood == null)
                continue;

            var bloodLevel = blood.Value.Comp.Solution.Volume.Int();

            if (!_bloodstream.TryModifyBloodLevel(target, -absorbed, bloodstream, false))
                continue;

            absorbed = Math.Min(absorbed, bloodLevel);
            _bloodstream.TryModifyBloodLevel(args.User, absorbed);
            _damageable.TryChangeDamage(args.User, new DamageSpecifier(_prototype.Index<DamageGroupPrototype>("Brute"), -absorbed));
            _damageable.TryChangeDamage(args.User, new DamageSpecifier(_prototype.Index<DamageGroupPrototype>("Burn"), -absorbed));
            _damageable.TryChangeDamage(args.User, new DamageSpecifier(_prototype.Index<DamageGroupPrototype>("Airloss"), -absorbed));
        }
    }

    private void OnMeleeHit(EntityUid uid, BloodAbsorbComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0
            || args.HitEntities[0] != args.User
            || !component.BloodLust
            || !TryComp(args.User, out BloodstreamComponent? bloodstream))
            return;

        EnsureComp<BloodLustComponent>(args.User);
        _bloodstream.TryModifyBleedAmount(args.User, bloodstream.MaxBleedAmount, bloodstream);
    }
}
