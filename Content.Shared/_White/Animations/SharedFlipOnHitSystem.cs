using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._White.Animations;

public abstract class SharedFlipOnHitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlipOnHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<FlipOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.HitEntities.Count == 0)
            return;

        if (TryComp(ent, out ItemToggleComponent? itemToggle) && !itemToggle.Activated)
            return;

        var target = ent.Comp.ApplyToSelf
            ? args.User
            : args.HitEntities[0];

        if (_standingState.IsDown(args.User))
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        PlayAnimation(args.User, target);
    }

    protected abstract void PlayAnimation(EntityUid user, EntityUid target);
}

[Serializable, NetSerializable]
public sealed class FlipOnHitEvent(NetEntity user, NetEntity target) : EntityEventArgs
{
    public NetEntity User = user;
    public NetEntity Target = target;
}
