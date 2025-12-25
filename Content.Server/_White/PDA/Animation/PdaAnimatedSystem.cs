using Content.Shared._White.PDA.Animation;
using Content.Shared.PDA;
using Content.Shared.UserInterface;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._White.PDA.Animation;

/// <summary>
/// Server system for animated PDA with delays
/// </summary>
public sealed class PdaAnimatedSystem : SharedPdaAnimatedSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaAnimatedComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<PdaAnimatedComponent, BoundUIClosedEvent>(OnUIClose);
    }

    private void OnOpenAttempt(EntityUid uid, PdaAnimatedComponent comp, ActivatableUIOpenAttemptEvent args)
    {
        if (comp.AnimationState == PdaAnimationState.Open)
            return;

        if (comp.AnimationState is PdaAnimationState.Opening or PdaAnimationState.Closing)
        {
            args.Cancel();
            return;
        }

        if (comp.AnimationState == PdaAnimationState.Closed)
        {
            args.Cancel();

            comp.AnimationState = PdaAnimationState.Opening;
            comp.AnimatingUser = args.User;
            Dirty(uid, comp);
            UpdateAppearance(uid, comp);

            if (TryComp<PdaComponent>(uid, out var pda))
            {
                pda.Enabled = true;
                pda.Screen = true;
                Appearance.SetData(uid, PdaVisuals.Enabled, true);
                Appearance.SetData(uid, PdaVisuals.Screen, true);
                Dirty(uid, pda);
            }

            Timer.Spawn(TimeSpan.FromSeconds(comp.OpeningDuration), () =>
            {
                if (!Deleted(uid) &&
                    TryComp<PdaAnimatedComponent>(uid, out var animComp) &&
                    animComp.AnimationState == PdaAnimationState.Opening)
                {
                    animComp.AnimationState = PdaAnimationState.Open;
                    Dirty(uid, animComp);
                    UpdateAppearance(uid, animComp);

                    if (animComp.AnimatingUser != null && _uiSystem.HasUi(uid, PdaUiKey.Key))
                        _uiSystem.TryOpenUi(uid, PdaUiKey.Key, animComp.AnimatingUser.Value);
                }
            });
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PdaAnimatedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AnimationState == PdaAnimationState.Closing && !_uiSystem.IsUiOpen(uid, PdaUiKey.Key))
            {
                if (!comp.ClosingAnimationStarted)
                {
                    comp.ClosingAnimationStarted = true;
                    Dirty(uid, comp);

                    Timer.Spawn(TimeSpan.FromSeconds(comp.ClosingDuration), () =>
                    {
                        if (!Deleted(uid) &&
                            TryComp<PdaAnimatedComponent>(uid, out var animComp) &&
                            animComp.AnimationState == PdaAnimationState.Closing)
                        {
                            animComp.AnimationState = PdaAnimationState.Closed;
                            animComp.AnimatingUser = null;
                            animComp.ClosingAnimationStarted = false;
                            Dirty(uid, animComp);
                            UpdateAppearance(uid, animComp);
                        }
                    });
                }
            }
            else if (comp.AnimationState != PdaAnimationState.Closing)
            {
                comp.ClosingAnimationStarted = false;
            }
        }
    }

    private void OnUIClose(EntityUid uid, PdaAnimatedComponent comp, BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(PdaUiKey.Key))
            return;

        if (comp.AnimationState == PdaAnimationState.Open)
        {
            comp.AnimationState = PdaAnimationState.Closing;
            comp.AnimatingUser = args.Actor;
            comp.ClosingAnimationStarted = true;
            Dirty(uid, comp);
            UpdateAppearance(uid, comp);

            Timer.Spawn(TimeSpan.FromSeconds(comp.ClosingDuration), () =>
            {
                if (!Deleted(uid) &&
                    TryComp<PdaAnimatedComponent>(uid, out var animComp) &&
                    animComp.AnimationState == PdaAnimationState.Closing)
                {
                    animComp.AnimationState = PdaAnimationState.Closed;
                    animComp.AnimatingUser = null;
                    Dirty(uid, animComp);
                    UpdateAppearance(uid, animComp);

                    if (TryComp<PdaComponent>(uid, out var pda))
                    {
                        pda.Enabled = false;
                        pda.Screen = false;
                        Appearance.SetData(uid, PdaVisuals.Enabled, false);
                        Appearance.SetData(uid, PdaVisuals.Screen, false);
                        Dirty(uid, pda);
                    }
                }
            });
        }
    }
}
