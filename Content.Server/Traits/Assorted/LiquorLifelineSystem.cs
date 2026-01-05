using Content.Server._White.Body.Systems;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Organs.Metabolizer;


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
            foreach (var metabolismGroup in metabolizer.Comp2.Stages) // WD EDIT
            {
                // Add the LiquorLifeline metabolizer type to the liver and equivalent organs.
                if (metabolismGroup.Key == "Absorption") // WD EDIT
                    metabolizer.Comp2.Types.Add("LiquorLifeline"); // WD EDIT
            }
        }
    }
}
