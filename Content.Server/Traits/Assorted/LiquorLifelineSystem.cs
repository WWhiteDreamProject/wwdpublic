using Content.Server._White.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared._White.Body.Components;

namespace Content.Server.Traits.Assorted;

public sealed class LiquorLifelineSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LiquorLifelineComponent, ComponentInit>(OnSpawn);
    }

    private void OnSpawn(Entity<LiquorLifelineComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<BodyComponent>(entity, out var body))
            return;

        if (!_bodySystem.TryGetOrgans<MetabolizerComponent>((entity, body), out var metabolizers, OrganType.Liver)) // WD EDIT
            return;

        foreach (var metabolizer in metabolizers)
        {
            if (metabolizer.Comp2.MetabolizerTypes is null // WD EDIT
                || metabolizer.Comp2.MetabolismGroups is null) // WD EDIT
                continue;

            foreach (var metabolismGroup in metabolizer.Comp2.MetabolismGroups) // WD EDIT
            {
                // Add the LiquorLifeline metabolizer type to the liver and equivalent organs.
                if (metabolismGroup.Id == "Alcohol")
                    metabolizer.Comp2.MetabolizerTypes.Add("LiquorLifeline"); // WD EDIT
            }
        }
    }
}
