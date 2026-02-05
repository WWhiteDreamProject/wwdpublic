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
    public AnimationInterpolationMode InterpolationMode;
}

[Serializable, DataDefinition]
public sealed partial class AnimationTrackComponentPropertyData : AnimationTrackPropertyData
{
    [DataField]
    [AlwaysPushInheritance]
    public string ComponentType;

    [DataField]
    [AlwaysPushInheritance]
    public string Property;
}

[Serializable, DataDefinition]
public sealed partial class AnimationTrackControlPropertyData : AnimationTrackPropertyData
{
    [DataField]
    [AlwaysPushInheritance]
    public string Property;
}

[Serializable, DataDefinition]
public sealed partial class AnimationTrackPlaySoundData : AnimationTrackData;

[Serializable, DataDefinition]
public sealed partial class AnimationTrackSpriteFlickData : AnimationTrackData
{
    [DataField]
    [AlwaysPushInheritance]
    public Enum LayerKey;
}

