using System;
using Content.Server.Announcements.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Pinpointer;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Localizations;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    // WD EDIT START
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    // WD EDIT END

    protected override void Started(EntityUid uid, RandomSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        // WWDP-EDIT-START
        int spawnCount = _random.Next(comp.MinCount, comp.MaxCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var coords))
                continue;

            Sawmill.Info($"Spawning {comp.Prototype} at {coords}");
            var entity = Spawn(comp.Prototype, coords);

            if (!comp.Announce || comp.LocId == null)
                continue;

            var grid = _transform.GetGrid(entity);
            if (grid == null)
                continue;
            var gridXform = Transform(grid.Value);

            var entityPos = _transform.GetWorldPosition(entity);
            var (gridPos, gridRot) = _transform.GetWorldPositionRotation(gridXform);

            var physicsQuery = GetEntityQuery<PhysicsComponent>();

            var entityCOM = entityPos;
            var gridCOM = Robust.Shared.Physics.Transform.Mul(new Transform(gridPos, gridRot),
                physicsQuery.GetComponent(grid.Value).LocalCenter);

            var mapDiff = entityCOM - gridCOM;
            var angle = mapDiff.ToWorldAngle();
            angle -= gridRot;

            var direction = ContentLocalizationManager.FormatDirection(angle.GetDir());
            var location = FormattedMessage.RemoveMarkupPermissive(
                _navMap.GetNearestBeaconString((entity, Transform(entity))));

            _announcer.SendAnnouncement(
                _announcer.GetAnnouncementId("Attention"),
                comp.LocId,
                localeArgs:
                [
                    ("direction", direction),
                    ("location", location)
                ]
            );

        // WWDP-EDIT-END
        }
    }
}
