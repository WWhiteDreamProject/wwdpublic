using Content.Shared._White.PDA.Animation;
using Content.Server.Interaction;
using Content.Shared.PDA;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server._White.PDA.Animation;

/// <summary>
/// Server system for animated PDA with delays
/// </summary>
public sealed class PdaAnimatedSystem : SharedPdaAnimatedSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;

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

            // Skip animations in test environment
            if (comp.SkipAnimations)
            {
                comp.AnimationState = PdaAnimationState.Open;
                Dirty(uid, comp);
                UpdateAppearance(uid, comp);

                if (TryComp<PdaComponent>(uid, out var pdaComponent))
                {
                    pdaComponent.Enabled = true;
                    pdaComponent.Screen = true;
                    Appearance.SetData(uid, PdaVisuals.Enabled, true);
                    Appearance.SetData(uid, PdaVisuals.Screen, true);
                    Dirty(uid, pdaComponent);
                }

                _uiSystem.TryOpenUi(uid, PdaUiKey.Key, args.User);
                return;
            }

            comp.AnimationState = PdaAnimationState.Opening;
            comp.AnimatingUser = args.User;
            comp.AnimationTimeAccumulator = 0f;
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
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PdaAnimatedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Skip anim in test
            if (comp.SkipAnimations)
                continue;

            if (comp.AnimationState == PdaAnimationState.Opening)
            {
                comp.AnimationTimeAccumulator += frameTime;

                if (comp.AnimationTimeAccumulator >= comp.OpeningDuration)
                {
                    if (comp.AnimatingUser != null &&
                        !TerminatingOrDeleted(uid) &&
                        !TerminatingOrDeleted(comp.AnimatingUser.Value) &&
                        _uiSystem.HasUi(uid, PdaUiKey.Key) &&
                        !_uiSystem.IsUiOpen(uid, PdaUiKey.Key, comp.AnimatingUser.Value) &&
                        _interactionSystem.InRangeUnobstructed(comp.AnimatingUser.Value, uid))
                    {
                        comp.AnimationState = PdaAnimationState.Open;
                        comp.AnimationTimeAccumulator = 0f;
                        Dirty(uid, comp);
                        UpdateAppearance(uid, comp);

                        _uiSystem.TryOpenUi(uid, PdaUiKey.Key, comp.AnimatingUser.Value);
                    }
                    else
                    {
                        comp.AnimationState = PdaAnimationState.Closing;
                        comp.AnimationTimeAccumulator = 0f;
                        Dirty(uid, comp);
                        UpdateAppearance(uid, comp);
                    }
                }
            }
            else if (comp.AnimationState == PdaAnimationState.Closing)
            {
                comp.AnimationTimeAccumulator += frameTime;

                if (comp.AnimationTimeAccumulator >= comp.ClosingDuration)
                {
                    comp.AnimationState = PdaAnimationState.Closed;
                    comp.AnimatingUser = null;
                    comp.AnimationTimeAccumulator = 0f;
                    Dirty(uid, comp);
                    UpdateAppearance(uid, comp);

                    if (TryComp<PdaComponent>(uid, out var pda))
                    {
                        pda.Enabled = false;
                        pda.Screen = false;
                        Appearance.SetData(uid, PdaVisuals.Enabled, false);
                        Appearance.SetData(uid, PdaVisuals.Screen, false);
                        Dirty(uid, pda);
                    }
                }
            }
        }
    }

    private void OnUIClose(EntityUid uid, PdaAnimatedComponent comp, BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(PdaUiKey.Key))
            return;

        if (comp.AnimationState == PdaAnimationState.Open)
        {
            if (comp.SkipAnimations)
            {
                comp.AnimationState = PdaAnimationState.Closed;
                comp.AnimatingUser = null;
                Dirty(uid, comp);
                UpdateAppearance(uid, comp);

                if (TryComp<PdaComponent>(uid, out var pda))
                {
                    pda.Enabled = false;
                    pda.Screen = false;
                    Appearance.SetData(uid, PdaVisuals.Enabled, false);
                    Appearance.SetData(uid, PdaVisuals.Screen, false);
                    Dirty(uid, pda);
                }
                return;
            }

            comp.AnimationState = PdaAnimationState.Closing;
            comp.AnimatingUser = args.Actor;
            comp.AnimationTimeAccumulator = 0f;
            Dirty(uid, comp);
            UpdateAppearance(uid, comp);
        }
    }
}
