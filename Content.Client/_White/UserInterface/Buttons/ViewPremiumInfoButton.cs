using Content.Client._White.UserInterface.Windows;
using Robust.Client.Animations;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._White.UserInterface.Buttons;

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
                        new AnimationTrackProperty.KeyFrame(Color.Yellow, 0f),
                        new AnimationTrackProperty.KeyFrame(Color.White, 1f)
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

