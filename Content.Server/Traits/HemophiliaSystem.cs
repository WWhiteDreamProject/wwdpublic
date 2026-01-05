using Content.Shared._White.Body.Bloodstream.Systems;

namespace Content.Server.Traits.Assorted;

public sealed class HemophiliaSystem : EntitySystem
{
    // WD EDIT START
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HemophiliaComponent, BleedModifierEvent>(OnBleedModifier);
    }

    private void OnBleedModifier(Entity<HemophiliaComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedReductionAmount *= ent.Comp.BleedReductionMultiplier;
        args.Bleeding *= ent.Comp.BleedAmountMultiplier;
    }
    // WD EDIT END
}
