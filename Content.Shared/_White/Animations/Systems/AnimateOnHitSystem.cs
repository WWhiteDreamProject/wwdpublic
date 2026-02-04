using Content.Shared._White.Animations.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._White.Animations.Systems;

public sealed class AnimateOnHitSystem : EntitySystem
{
    [Dependency] private readonly SharedWhiteAnimationPlayerSystem _whiteAnimationPlayer = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimateOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<AnimateOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (TryComp(ent, out ItemToggleComponent? itemToggle) && !itemToggle.Activated)
            return;

        var target = ent.Comp.ApplyToSelf ? args.User : args.HitEntities[0];

        if (_standingState.IsDown(target))
            return;

        _whiteAnimationPlayer.Play(target, ent.Comp.Animation);
    }
}
