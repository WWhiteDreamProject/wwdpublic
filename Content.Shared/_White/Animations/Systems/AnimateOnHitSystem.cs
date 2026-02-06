using Content.Shared._White.Animations.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._White.Animations.Systems;

public sealed class AnimateOnHitSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
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

        if (ent.Comp.ApplyToUser)
        {
            PlayAnimation(args.User, ent.Comp, args.User);
            return;
        }

        foreach (var target in args.HitEntities)
            PlayAnimation(target, ent.Comp, args.User);
    }

    private void PlayAnimation(EntityUid target, AnimateOnHitComponent component, EntityUid recipient)
    {
        if (_standingState.IsDown(target) || _entityWhitelist.IsWhitelistFail(component.Whitelist, target))
            return;

        _whiteAnimationPlayer.PlayPredicted(target, component.Animation, recipient);
    }
}
