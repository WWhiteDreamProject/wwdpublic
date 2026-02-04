using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._White.Animations.Prototypes;

[Prototype]
public sealed class AnimationPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AnimationPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [DataField(required: true)]
    public TimeSpan Length;

    [DataField(required: true)]
    public List<AnimationTrackData> AnimationTracksData = new();

    [DataField(required: true)]
    public string Key = string.Empty;

    [ViewVariables]
    public object Animation = null!;
}
