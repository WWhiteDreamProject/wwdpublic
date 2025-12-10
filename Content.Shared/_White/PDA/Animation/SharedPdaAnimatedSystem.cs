using Content.Shared.PDA;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Shared._White.PDA.Animation;

/// <summary>
/// Shared system for animated PDA
/// </summary>
public abstract class SharedPdaAnimatedSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    private void OnBeforeUIOpen(EntityUid uid, PdaAnimatedComponent comp, BeforeActivatableUIOpenEvent args)
    {
        // If already open or opening, don't start animation again
        if (comp.AnimationState is PdaAnimationState.Open or PdaAnimationState.Opening)
            return;

        // Start opening animation
        comp.AnimationState = PdaAnimationState.Opening;
        comp.AnimatingUser = args.User;
        Dirty(uid, comp);

        UpdateAppearance(uid, comp);
    }

    private void OnUIClose(EntityUid uid, PdaAnimatedComponent comp, BoundUIClosedEvent args)
    {
        // Check if this is the PDA UI
        if (!args.UiKey.Equals(PdaUiKey.Key))
            return;

        // Start closing animation
        if (comp.AnimationState == PdaAnimationState.Open)
        {
            comp.AnimationState = PdaAnimationState.Closing;
            comp.AnimatingUser = args.Actor;
            Dirty(uid, comp);

            UpdateAppearance(uid, comp);
        }
    }

    protected void UpdateAppearance(EntityUid uid, PdaAnimatedComponent comp)
    {
        Appearance.SetData(uid, PdaVisuals.AnimationState, comp.AnimationState);
    }
}
