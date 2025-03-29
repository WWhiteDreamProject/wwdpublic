using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared._White.Xenomorphs.Plasma.Components;

namespace Content.Server._White.Xenomorphs.Plasma;

public sealed class PlasmaSystem : SharedPlasmaSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlasmaVesselComponent>();
        while (query.MoveNext(out var uid, out var plasmaVessel))
        {
            plasmaVessel.Accumulator += frameTime;

            if (plasmaVessel.Accumulator <= 1)
                continue;

            plasmaVessel.Accumulator -= 1;

            if (plasmaVessel.Plasma < plasmaVessel.PlasmaRegenCap)
                ChangePlasmaAmount(uid, plasmaVessel.PlasmaPerSecond, plasmaVessel, true);
        }
    }
}
