using Content.Server.Power;
using Content.Shared.Emp;
using Robust.Shared.GameObjects;
using Content.Shared.Weapons.Melee.Events;
using Content.Server._White.EmpFlashlight;
using Content.Server.Emp;
using Robust.Shared.IoC;
using Content.Shared.Charges.Systems;
using Content.Shared.Charges.Components;

namespace Content.Server._White.EmpFlashlight;

public sealed class EmpHitSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpOnHitComponent, MeleeHitEvent>(HandleEmpHit);
    }

    public bool TryEmpHit(EntityUid uid, EmpOnHitComponent comp, MeleeHitEvent args)
    {
        LimitedChargesComponent? charges;
        if (!TryComp<LimitedChargesComponent>(uid, out charges))
            return false;

        if (_charges.IsEmpty(uid, charges))
            return false;

        if (charges != null && args.HitEntities.Count > 0)
        {
            _charges.UseCharge(uid,charges);
            return true;
        }

        return false;
    }

    private void HandleEmpHit(EntityUid uid, EmpOnHitComponent comp, MeleeHitEvent args)
    {
        if (!TryEmpHit(uid, comp, args))
            return;

        foreach (var affected in args.HitEntities)
        {
            _emp.EmpPulse(Transform(affected).MapPosition, comp.Range, comp.EnergyConsumption, comp.DisableDuration);
        }

        args.Handled = true;
    }
}

