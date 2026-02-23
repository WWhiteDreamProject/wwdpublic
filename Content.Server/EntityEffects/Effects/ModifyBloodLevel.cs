using Content.Server._White.Body.Bloodstream.Systems;
using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ModifyBloodLevel : EntityEffect
{
    [DataField]
    public bool Scaled = false;

    [DataField]
    public FixedPoint2 Amount = 1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-blood-level", ("chance", Probability),
            ("deltasign", MathF.Sign(Amount.Float())));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var blood))
        {
            var sys = args.EntityManager.System<BloodstreamSystem>();
            var amt = Amount;
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (Scaled)
                    amt *= reagentArgs.Quantity;
                amt *= reagentArgs.Scale;
            }

            sys.TryModifyBloodLevel((args.TargetEntity, blood), amt);
        }
    }
}
