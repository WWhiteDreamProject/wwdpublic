using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Throwing;

namespace Content.Shared._White.Collision.LayDown;

public sealed class LayDownOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedLayingDownSystem _layingDown = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LayDownOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<LayDownOnCollideComponent, ThrowDoHitEvent>(OnEntityHit);
    }

    private void OnEntityHit(Entity<LayDownOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void OnProjectileHit(Entity<LayDownOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void ApplyEffects(EntityUid target, LayDownOnCollideComponent component)
    {
        if (!Exists(target))
            return;

        _layingDown.TryLieDown(target, null, component.Behavior); // WD EDIT
    }
}
