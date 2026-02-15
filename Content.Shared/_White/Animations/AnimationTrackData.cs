using Robust.Shared.Animations;

namespace Content.Shared._White.Animations;

[Serializable]
public abstract class AnimationTrackData
{
    [DataField]
    [AlwaysPushInheritance]
    public List<KeyFrameData> KeyFrames = new();
}

[Serializable]
public abstract class AnimationTrackPropertyData : AnimationTrackData
{
    [DataField]
    [AlwaysPushInheritance]
    public AnimationInterpolationMode InterpolationMode = AnimationInterpolationMode.Linear;
}

[Serializable, DataDefinition]
public sealed partial class AnimationTrackComponentPropertyData : AnimationTrackPropertyData
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public string ComponentType;

    [DataField(required: true)]
    [AlwaysPushInheritance]
    public string Property;
}

[Serializable, DataDefinition]
public sealed partial class AnimationTrackPlaySoundData : AnimationTrackData;

[Serializable, DataDefinition]
public sealed partial class AnimationTrackSpriteFlickData : AnimationTrackData
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public Enum LayerKey;
}


