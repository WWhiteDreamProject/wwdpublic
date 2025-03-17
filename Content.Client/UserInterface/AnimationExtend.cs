using System.Linq;
using Robust.Client.Animations;
using Robust.Client.UserInterface;
using Robust.Shared.Animations;


namespace Content.Client.UserInterface;

public sealed class AnimationExtend<T>  : Control
{
    public TimeSpan Length;

    private readonly Action<T> _action;

    private readonly Guid _guid = Guid.NewGuid();

    public Action? AnimationIsCompleted;

    public Animation? Animation { get; private set; }

    private T _realValue = default!;

    private AnimationTrackControlProperty? _track;

    public AnimationTrackControlProperty? Track
    {
        get => _track;
        set
        {
            if(value is null) return;
            _track = value;
            _track.Property = nameof(Value);

            Length = _track.KeyFrames.Aggregate(TimeSpan.Zero,
                (span, frame) => span.Add(TimeSpan.FromSeconds(frame.KeyTime)));

            Animation = new Animation()
            {
                Length = Length, AnimationTracks = { _track }
            };
        }
    }

    [Animatable]
    public T Value
    {
        get => _realValue;
        set
        {
            _action(value);
            _realValue = value;
        }
    }

    public AnimationExtend(Action<T> action, Control parent, AnimationTrackControlProperty track)
    {
        _action = action;
        parent.AddChild(this);
        Track = track;

        AnimationCompleted += OnAnimationCompleted;
    }

    private void OnAnimationCompleted(string obj)
    {
        if(obj == _guid.ToString()) AnimationIsCompleted?.Invoke();
    }

    public void PlayAnimation()
    {
        if(Animation is null) return;
        PlayAnimation(Animation,_guid.ToString());
    }

    public bool HasRunningAnimation()
    {
        return HasRunningAnimation(_guid.ToString());
    }

}
