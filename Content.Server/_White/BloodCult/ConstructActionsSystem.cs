using Content.Shared._White.BloodCult.PhaseShift;
using Content.Shared._White.BloodCult.Spells;
using Content.Shared.StatusEffect;

namespace Content.Server._White.BloodCult;

public sealed class ConstructActionsSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PhaseShiftEvent>(OnPhaseShift);
    }

    private void OnPhaseShift(PhaseShiftEvent args)
    {
        if (args.Handled)
            return;

        if (_statusEffects.TryAddStatusEffect<PhaseShiftedComponent>(
            args.Performer,
            args.StatusEffectId,
            args.Duration,
            false))
            args.Handled = true;
    }
}
