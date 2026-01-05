using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Bloodstream.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._White.Other.BloodLust;

public sealed class BloodLustSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodLustComponent, AfterBleedingChangedEvent>(OnAfterBleedingChanged);
        SubscribeLocalEvent<BloodLustComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
        SubscribeLocalEvent<BloodLustComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnAfterBleedingChanged(EntityUid uid, BloodLustComponent component, AfterBleedingChangedEvent args)
    {
        if (args.Bleeding == 0f)
            RemComp<BloodLustComponent>(uid);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
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
        if (!TryComp(uid, out BloodstreamComponent? bloodstream) || bloodstream.MaximumBleeding == 0f)
            return 1f;

        return Math.Clamp((bloodstream.Bleeding / bloodstream.MaximumBleeding).Float(), 0f, 1f);
    }

    private float GetBloodLustMultiplier(float multiplier, float modifier)
    {
        return float.Lerp(1f, multiplier, modifier);
    }
}
