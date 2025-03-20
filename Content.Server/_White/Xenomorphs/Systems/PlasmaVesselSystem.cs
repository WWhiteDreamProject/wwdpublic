using Content.Shared._White.Xenomorphs.Components;
using Content.Shared._White.Xenomorphs.Systems;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class PlasmaVesselSystem : SharedPlasmaVesselSystem
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
