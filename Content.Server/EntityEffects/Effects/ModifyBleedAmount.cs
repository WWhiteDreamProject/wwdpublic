using Content.Server._White.Body.Bloodstream.Systems;
using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ModifyBleedAmount : EntityEffect
{
    [DataField]
    public bool Scaled = false;

    [DataField]
    public float Amount = -1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-bleed-amount", ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var blood))
        {
            var sys = args.EntityManager.System<BloodstreamSystem>();
            var amt = Amount;
            if (args is EntityEffectReagentArgs reagentArgs) {
                if (Scaled)
                    amt *= reagentArgs.Quantity.Float();
                amt *= reagentArgs.Scale.Float();
            }

            sys.TryModifyBleedAmount((args.TargetEntity, blood), amt); // WD EDIT
        }
    }
}
