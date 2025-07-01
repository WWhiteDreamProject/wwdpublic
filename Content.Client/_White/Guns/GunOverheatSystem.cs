using Content.Shared._White.Guns;
using Robust.Client.Audio;
using Robust.Shared.Player;

namespace Content.Client._White.Guns;
public sealed class GunOverheatSystem : SharedGunOverheatSystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GunOverheatComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.TimeToStartVenting == 0)
                continue;

            var timeSinceLastFire = (_timing.CurTime - comp.LastFire).TotalSeconds;
            switch (comp.VentingStage) {
                case 0:
                    if (timeSinceLastFire > comp.TimeToStartVenting)
                    {
                        comp.VentingStage = 1;
                        _audio.PlayLocal(comp.VentingSound, uid, uid);
                    }
                    continue;

                case 1:
                    if (GetCurrentTemperature(comp) == 0 &&
                       timeSinceLastFire > comp.TimeToStartVenting + comp.VentingFinishedSoundTimeOffset)
                    {
                        comp.VentingStage = 2;
                        _audio.PlayLocal(comp.VentingFinishedSound, uid, uid);
                    }
                    continue;
                case 2:
                    continue;
            }
        }
    }
}
