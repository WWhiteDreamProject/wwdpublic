using Content.Shared._NC.Cyberware.Components;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Slippery;
using Content.Shared.StatusEffect;

namespace Content.Shared._NC.Cyberware.Systems;

/// <summary>
///     Handles shared relaying of component properties from cyberware to host.
/// </summary>
public sealed class SharedCyberwareRelaySystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<CyberwareComponent, BeforeStatusEffectAddedEvent>(OnBeforeStatusEffect);
        
        // Use VeryLate to run after all other speed modifiers have been applied
        SubscribeLocalEvent<CyberwareSlowImmunityComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed, order: EventOrder.VeryLate);
    }

    private void OnSlipAttempt(EntityUid uid, CyberwareComponent component, SlipAttemptEvent args)
    {
        foreach (var implantUid in component.InstalledImplants.Values)
        {
            if (_entManager.HasComponent<NoSlipComponent>(implantUid))
            {
                args.NoSlip = true;
                return;
            }
        }
    }

    private void OnBeforeStatusEffect(EntityUid uid, CyberwareComponent component, ref BeforeStatusEffectAddedEvent args)
    {
        foreach (var implantUid in component.InstalledImplants.Values)
        {
            if (!_entManager.TryGetComponent<SturdyComponent>(implantUid, out var sturdy))
                continue;

            if (sturdy.KnockdownImmunity && args.Key == "KnockedDown")
            {
                args.Cancelled = true;
                return;
            }

            if (sturdy.StunImmunity && args.Key == "Stun")
            {
                args.Cancelled = true;
                return;
            }
        }
    }

    private void OnRefreshSpeed(EntityUid uid, CyberwareSlowImmunityComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        // If the resulting multiplier is less than 1.0 (slowdown), 
        // we multiply it by the reciprocal to bring it back to exactly 1.0.
        
        if (args.WalkSpeedModifier < 1.0f)
        {
            args.ModifySpeed(1.0f / args.WalkSpeedModifier, 1.0f);
        }

        if (args.SprintSpeedModifier < 1.0f)
        {
            args.ModifySpeed(1.0f, 1.0f / args.SprintSpeedModifier);
        }
    }
}
