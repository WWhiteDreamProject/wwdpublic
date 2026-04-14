// D:\projects\night-station\Content.Shared\_NC\Cyberware\Systems\SharedCyberwareRelaySystem.cs
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._Shitmed.Body.Components;
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
    }

    private void OnSlipAttempt(EntityUid uid, CyberwareComponent component, SlipAttemptEvent args)
    {
        // If any installed implant has NoSlipComponent, prevent slipping.
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
}
