using Content.Shared._White.Stealth;
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
            if (time < plasmaVessel.NextPointsAt)
                continue;

            plasmaVessel.NextPointsAt = time + TimeSpan.FromSeconds(1);

            var plasma = plasmaVessel.PlasmaPerSecondOffWeed;
            if (TryComp<XenomorphComponent>(uid, out var xenomorph) && xenomorph.OnWeed)
                plasma = plasmaVessel.PlasmaPerSecondOnWeed;

            if (TryComp<StealthOnWalkComponent>(uid, out var stealthOnWalk) && stealthOnWalk.Stealth)
                plasma -= stealthOnWalk.PlasmaCost;

            ChangePlasmaAmount(uid, plasma, plasmaVessel);
        }
    }
}
