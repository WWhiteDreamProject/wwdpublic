// D:\projects\night-station\Content.Shared\_NC\Cyberware\Systems\SharedCyberwareRelaySystem.cs
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Slippery;

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
}
