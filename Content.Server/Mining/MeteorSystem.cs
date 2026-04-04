using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Projectiles;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles; // WWDP edit
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Mining;

public sealed class MeteorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        // WWDP edit start
        SubscribeLocalEvent<MeteorComponent, StartCollideEvent>(OnCollide, after: [typeof(ProjectileSystem)]);
        // WWDP edit end
    }

    private void OnCollide(EntityUid uid, MeteorComponent component, ref StartCollideEvent args)
    {
        if (TerminatingOrDeleted(args.OtherEntity) || TerminatingOrDeleted(uid))
            return;

        if (component.HitList.Contains(args.OtherEntity))
            return;

        // WWDP edit start
        // If the meteor has a penetrating projectile with remaining penetrations,
        // skip all meteor collision logic (including self-damage) to let penetration work first.
        // When penetrations are exhausted (ProjectileSpent), meteor behavior resumes
        // and self-damage triggers Destructible normally.
        if (TryComp<ProjectileComponent>(uid, out var projectile)
            && projectile.Penetrate
            && projectile.MaxPenetrations > 0
            && !projectile.ProjectileSpent)
        {
            if (!TerminatingOrDeleted(args.OtherEntity))
                component.HitList.Add(args.OtherEntity);
            return;
        }
        // WWDP edit end

        FixedPoint2 threshold;
        if (_mobThreshold.TryGetDeadThreshold(args.OtherEntity, out var mobThreshold))
        {
            threshold = mobThreshold.Value;
            if (HasComp<ActorComponent>(args.OtherEntity))
                _adminLog.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.OtherEntity):player} was struck by meteor {ToPrettyString(uid):ent} and killed instantly.");
        }
        else if (_destructible.TryGetDestroyedAt(args.OtherEntity, out var destroyThreshold))
        {
            threshold = destroyThreshold.Value;
        }
        else
        {
            threshold = FixedPoint2.MaxValue;
        }
        var otherEntDamage = CompOrNull<DamageableComponent>(args.OtherEntity)?.TotalDamage ?? FixedPoint2.Zero;
        // account for the damage that the other entity has already taken: don't overkill
        threshold -= otherEntDamage;

        // The max amount of damage our meteor can take before breaking.
        var maxMeteorDamage = _destructible.DestroyedAt(uid) - CompOrNull<DamageableComponent>(uid)?.TotalDamage ?? FixedPoint2.Zero;

        // Cap damage so we don't overkill the meteor
        var trueDamage = FixedPoint2.Min(maxMeteorDamage, threshold);

        var damage = component.DamageTypes * trueDamage;
        _damageable.TryChangeDamage(args.OtherEntity, damage, true, origin: uid);
        _damageable.TryChangeDamage(uid, damage);

        if (!TerminatingOrDeleted(args.OtherEntity))
            component.HitList.Add(args.OtherEntity);
    }
}
