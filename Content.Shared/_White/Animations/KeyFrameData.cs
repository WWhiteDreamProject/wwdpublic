using Robust.Shared.Audio;
using Robust.Shared.Utility;

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
    public string Type;

    [DataField]
    [AlwaysPushInheritance]
    public string Value;
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
