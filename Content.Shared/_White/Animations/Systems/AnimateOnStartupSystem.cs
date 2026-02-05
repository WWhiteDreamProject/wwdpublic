using Content.Shared._White.Animations.Components;

namespace Content.Shared._White.Animations.Systems;

public sealed class AnimateOnStartupSystem : EntitySystem
{
    [Dependency] private readonly SharedWhiteAnimationPlayerSystem _whiteAnimationPlayer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimateOnStartupComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<AnimateOnStartupComponent> ent, ref ComponentStartup args) =>
        _whiteAnimationPlayer.Play(ent, ent.Comp.Animation);
}
