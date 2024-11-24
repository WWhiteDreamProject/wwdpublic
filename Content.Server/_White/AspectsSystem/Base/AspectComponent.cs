using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._White.AspectsSystem.Base;

[RegisterComponent]
public sealed partial class AspectComponent : Component
{
    [DataField]
    public string? Name;

    [DataField]
    public string? Description;

    [DataField]
    public string? Requires;

    [DataField]
    public float Weight = 1.0f;

    [DataField("forbidden")]
    public bool IsForbidden;

    [DataField("hidden")]
    public bool IsHidden;

    [DataField]
    public SoundSpecifier? StartAudio;

    [DataField]
    public SoundSpecifier? EndAudio;

    [DataField]
    public TimeSpan StartDelay = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan StartTime;
}
