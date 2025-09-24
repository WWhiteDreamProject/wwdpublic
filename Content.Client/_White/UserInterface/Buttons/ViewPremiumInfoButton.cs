using Content.Client._White.UI.Windows;
using Robust.Client.Animations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._White.UI.Buttons;

public sealed class ViewPremiumInfoButton : Button
{
    private WindowTracker<PremiumPassWindow> _premWindow = new();
    public ViewPremiumInfoButton() : base()
    {
        OnPressed += Pressed;

        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(1.5f),
            AnimationTracks =
            {
                new AnimationTrackControlProperty()
                {
                    Property = nameof(Modulate),
                    InterpolationMode = Robust.Shared.Animations.AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackControlProperty.KeyFrame(Color.Yellow, 0f),
                        new AnimationTrackControlProperty.KeyFrame(Color.White, 1f)
                    }
                }
            }
        };

        PlayAnimation(anim, "hurr durr");
        AnimationCompleted += _ => PlayAnimation(anim, "hurr durr");
    }

    private void Pressed(ButtonEventArgs args)
    {
        _premWindow.TryOpenCentered();
    }
}

