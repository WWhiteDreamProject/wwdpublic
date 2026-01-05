using Content.Server._White.Body.Respirator.Components;
using Content.Server._White.Body.Respirator.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class Oxygenate : EntityEffect
{
    [DataField]
    public float Factor = 1f;

    // JUSTIFICATION: This is internal magic that players never directly interact with.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {

        var multiplier = 1f;
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            multiplier = reagentArgs.Quantity.Float();
        }

        if (args.EntityManager.TryGetComponent<RespiratorComponent>(args.TargetEntity, out var resp))
        {
            var respSys = args.EntityManager.System<RespiratorSystem>();
            respSys.ChangeSaturationLevel((args.TargetEntity, resp), multiplier * Factor * 0.01f); // WD EDIT
        }
    }
}
