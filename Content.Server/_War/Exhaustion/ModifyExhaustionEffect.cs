using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._War.Exhaustion;

/// <summary>
/// Metabolism effect that modifies entity's exhaustion value
/// </summary>
[UsedImplicitly]
public sealed partial class ModifyExhaustion : EntityEffect
{
    /// <summary>
    /// How much to change exhaustion by. Negative values heal exhaustion.
    /// </summary>
    [DataField("amount")]
    public FixedPoint2 Amount;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    /// <inheritdoc/>
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        var entityManager = reagentArgs.EntityManager;

        if (!entityManager.TryGetComponent(reagentArgs.TargetEntity, out ExhaustionComponent? exhaustion))
            return;

        var scaled = Amount * reagentArgs.Quantity;

        var exhaustionSystem = entityManager.System<ExhaustionSystem>();
        exhaustionSystem.ModifyExhaustion(args.TargetEntity, scaled, exhaustion);
    }
}
