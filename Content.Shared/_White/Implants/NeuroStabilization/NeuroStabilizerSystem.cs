using Content.Shared.Electrocution;
using Content.Shared.Damage.Systems;

namespace Content.Shared._White.Implants.NeuroStabilization;

public sealed class NeuroStabilizerSystem : EntitySystem
{
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeuroStabilizationComponent, BeforeStaminaDamageEvent>(BeforeStaminaDamage);
    }

    private void BeforeStaminaDamage(EntityUid uid, NeuroStabilizationComponent component, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled = true;

        if (component.Electrocution)
        {
            _electrocution.TryDoElectrocution(uid, null,
                (int) MathF.Round(args.Value * component.DamageModifier), component.TimeElectrocution,
                false, 0.5f, null, true);
        }
    }
}
