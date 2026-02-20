using Robust.Shared.Audio;

namespace Content.Shared._White.Animations;

[Serializable]
public abstract class KeyFrameData
{
    [DataField]
    [AlwaysPushInheritance]
    public float Keyframe;
}

[Serializable, DataDefinition]
public sealed partial class KeyFramePropertyData : KeyFrameData
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public string Value;
}

[Serializable, DataDefinition]
public sealed partial class KeyFrameSoundData : KeyFrameData
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public SoundSpecifier Sound;
}

[Serializable, DataDefinition]
public sealed partial class KeyFrameSpriteFlickData : KeyFrameData
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public string State;
}
