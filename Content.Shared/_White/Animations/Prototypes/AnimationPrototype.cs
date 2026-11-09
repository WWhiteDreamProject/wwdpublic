using Robust.Shared.Prototypes;

namespace Content.Shared._White.Animations.Prototypes;

[Prototype]
public sealed partial class AnimationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public TimeSpan? Length;

    [DataField(required: true)]
    public List<AnimationTrackData> AnimationTracksData = new();

    [DataField(required: true)]
    public string Key = string.Empty;
}
