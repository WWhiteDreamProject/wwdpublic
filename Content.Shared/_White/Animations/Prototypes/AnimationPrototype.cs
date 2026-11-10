using Robust.Shared.Prototypes;

namespace Content.Shared._White.Animations.Prototypes;

[Prototype]
public sealed class AnimationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public TimeSpan? Length;

    [DataField(required: true)]
    public List<AnimationTrackData> AnimationTracksData = new();

    [DataField(required: true)]
    public string Key = string.Empty;
}
