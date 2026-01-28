using Content.Shared.Chemistry.Reagent;
using Content.Server.Psionics;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.Abilities.Psionics;

namespace Content.Server._White.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed partial class ChemGivePsionic : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-chem-give-psionic", ("chance", Probability));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is not EntityEffectReagentArgs _)
                return;

            args.EntityManager.EnsureComponent<PsionicComponent>(args.TargetEntity);
        }
    }
}
