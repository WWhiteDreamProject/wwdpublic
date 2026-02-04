using Content.Shared._White.Helpers;
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
    [DataField]
    [AlwaysPushInheritance]
    public DynamicValue Value;
}

[Serializable, DataDefinition]
public sealed partial class KeyFrameSoundData : KeyFrameData
{
    [DataField]
    [AlwaysPushInheritance]
    public SoundSpecifier Sound;
}

[Serializable, DataDefinition]
public sealed partial class KeyFrameSpriteFlickData : KeyFrameData
{
    [DataField]
    [AlwaysPushInheritance]
    public string State;
}
