using System;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!; // WWDP
    protected override void Started(EntityUid uid, RandomSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        // WWDP-EDIT-START
        int spawnCount = _random.Next(comp.MinCount, comp.MaxCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            if (TryFindRandomTile(out _, out _, out _, out var coords))
            {
                Sawmill.Info($"Spawning {comp.Prototype} at {coords}");
                Spawn(comp.Prototype, coords);
            }
        // WWDP-EDIT-END
        }
    }
}
