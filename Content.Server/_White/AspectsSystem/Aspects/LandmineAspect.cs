using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class LandmineAspect : AspectSystem<LandmineAspectComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Added(EntityUid uid, LandmineAspectComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        SpawnMines(component);
    }

    private void SpawnMines(LandmineAspectComponent component)
    {
        var minMines = _random.Next(component.Min, component.Max);

        for (var i = 0; i < minMines; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var targetCoords))
                break;

            EntityManager.SpawnEntity("LandMineAspectExplosive", targetCoords);
        }
    }
}
