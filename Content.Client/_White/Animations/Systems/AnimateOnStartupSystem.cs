using Content.Shared._White.Animations.Components;

namespace Content.Client._White.Animations.Systems;

public sealed class AnimateOnStartupSystem : EntitySystem
{
    [Dependency] private readonly WhiteAnimationPlayerSystem _whiteAnimationPlayer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimateOnStartupComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<AnimateOnStartupComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp is { Played: true, Force: false, })
            return;

        _whiteAnimationPlayer.Play(ent, ent.Comp.Animation);
        ent.Comp.Played = true;
    }
}
