using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        /*
         * On server because client doesn't want to predict other's guns.
         */

        // Automatic firing without stopping if the AutoShootGunComponent component is exist and enabled

        // WWDP EDIT START
        var autoShootGunQuery = EntityQueryEnumerator<AutoShootGunComponent>();
        while (autoShootGunQuery.MoveNext(out var uid, out var autoShoot))
        {
            if (!autoShoot.Enabled
                || !TryGetGun(uid, out var gunUid, out var gun)
                || gun.NextFire > Timing.CurTime)
                continue;

            // uid will either be same as gunUid, or it will be something that is holding (and shooting) the gun.
            AttemptShoot(uid, gunUid, gun);
        }
        // WWDP EDIT END

        var query = EntityQueryEnumerator<GunComponent>();

        while (query.MoveNext(out var uid, out var gun))
        {
            if (gun.NextFire > Timing.CurTime)
                continue;

            // WWDP EDIT START
            if (TryComp(uid, out AutoShootGunComponent? autoShoot) && autoShoot.Enabled)
                continue;
            // WWDP EDIT END

            if (gun.BurstActivated)
            {
                var parent = _transform.GetParentUid(uid);
                if (HasComp<DamageableComponent>(parent))
                    AttemptShoot(parent, uid, gun, gun.ShootCoordinates ?? new EntityCoordinates(uid, gun.DefaultDirection));
                else
                    AttemptShoot(uid, gun);
            }
        }
    }
}
