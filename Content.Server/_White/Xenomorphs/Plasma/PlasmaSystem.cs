using Content.Shared._White.Xenomorphs.Components;
using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Plasma;

public sealed class PlasmaSystem : SharedPlasmaSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<PlasmaVesselComponent>();
        while (query.MoveNext(out var uid, out var plasmaVessel))
        {
            if (plasmaVessel.Plasma == plasmaVessel.MaxPlasma
                || time < plasmaVessel.LastPointsAt + TimeSpan.FromSeconds(1))
                continue;

            plasmaVessel.LastPointsAt = time;

            var plasmaPerSecond = plasmaVessel.PlasmaPerSecondOffWeed;
            if (TryComp<XenomorphComponent>(uid, out var alien) && alien.OnWeed)
                plasmaPerSecond = plasmaVessel.PlasmaPerSecondOnWeed;

            ChangePlasmaAmount(uid, plasmaPerSecond, plasmaVessel);
        }
    }
}
