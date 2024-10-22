using Content.Server.Body.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._White.Other.BloodLust;

public sealed class BloodLustSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BloodLustComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
        SubscribeLocalEvent<BloodLustComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnGetMeleeAttackRate(EntityUid uid, BloodLustComponent component, GetMeleeAttackRateEvent args)
    {
        if (!TryComp(args.User, out BloodLustComponent? bloodLust))
            return;

        args.Multipliers *= GetBloodLustMultiplier(bloodLust.AttackRateModifier, GetBloodLustModifier(args.User));
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, BloodLustComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = GetBloodLustModifier(uid);
        args.ModifySpeed(GetBloodLustMultiplier(component.WalkModifier, modifier),
            GetBloodLustMultiplier(component.SprintModifier, modifier));
    }

    private float GetBloodLustModifier(EntityUid uid)
    {
        if (!TryComp(uid, out BloodstreamComponent? bloodstream) || bloodstream.MaxBleedAmount == 0f)
            return 1f;

        return Math.Clamp(bloodstream.BleedAmount / bloodstream.MaxBleedAmount, 0f, 1f);
    }

    private float GetBloodLustMultiplier(float multiplier, float modifier)
    {
        return float.Lerp(1f, multiplier, modifier);
    }
}
