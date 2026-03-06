using Content.Server._White.Body.Bloodstream.Systems;
using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.EntityEffects.EffectConditions;

public sealed partial class UniqueBloodstreamChemThreshold : EntityEffectCondition
{
    [DataField]
    public int Max = int.MaxValue;

    [DataField]
    public int Min = -1;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var blood))
        {
            // WD EDIT START
            var reagents = args.EntityManager.System<BloodstreamSystem>().GetChemicals((args.TargetEntity, blood));
            return reagents.Count > Min && reagents.Count < Max;
            // WD EDIT END
        }
        throw new NotImplementedException();
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-unique-bloodstream-chem-threshold",
            ("max", Max),
            ("min", Min));
    }
}
