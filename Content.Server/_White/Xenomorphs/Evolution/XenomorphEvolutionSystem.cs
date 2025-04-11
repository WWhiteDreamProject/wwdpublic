using Content.Shared._White.Xenomorphs.Evolution;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Evolution;

public sealed class XenomorphEvolutionSystem : SharedXenomorphEvolutionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<XenomorphEvolutionComponent>();
        while (query.MoveNext(out var uid, out var alienEvolution))
        {
            if (alienEvolution.Points == alienEvolution.Max
                || time < alienEvolution.LastPointsAt + TimeSpan.FromSeconds(1))
                continue;

            alienEvolution.LastPointsAt = time;
            alienEvolution.Points += alienEvolution.PointsPerSecond;
            Dirty(uid, alienEvolution);

            if (alienEvolution.Points != alienEvolution.Max)
                continue;

            Popup.PopupEntity(Loc.GetString("xenomorphs-evolution-ready"), uid, uid, PopupType.Large);
        }
    }
}
