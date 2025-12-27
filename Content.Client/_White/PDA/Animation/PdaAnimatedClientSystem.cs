using Content.Client._White.PDA.Animation;
using Content.Shared._White.PDA.Animation;
using Content.Shared.UserInterface;
using Robust.Shared.Player;

namespace Content.Client._White.PDA.Animation;

/// <summary>
/// Client system to prevent UI flash during animation
/// </summary>
public sealed class PdaAnimatedClientSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaAnimatedComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
    }

    private void OnOpenAttempt(EntityUid uid, PdaAnimatedComponent comp, ActivatableUIOpenAttemptEvent args)
    {
        if (comp.AnimationState != PdaAnimationState.Open)
            args.Cancel();
    }
}
