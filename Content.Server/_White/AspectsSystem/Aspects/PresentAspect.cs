using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class PresentAspect : AspectSystem<PresentAspectComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Added(EntityUid uid, PresentAspectComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        SpawnPresents(component);
    }

    private void SpawnPresents(PresentAspectComponent component)
    {
        var minPresents = _random.Next(component.Min, component.Max);

        for (var i = 0; i < minPresents; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var targetCoords))
                break;

            EntityManager.SpawnEntity("PresentRandomUnsafe", targetCoords);
        }
    }
}
