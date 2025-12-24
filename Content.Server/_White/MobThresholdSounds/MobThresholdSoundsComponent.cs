using Robust.Shared.Audio;

namespace Content.Server._White.MobThresholdSounds;

[RegisterComponent]
public sealed partial class MobThresholdSoundsComponent : Component
{
    [DataField]
    public SoundSpecifier DeathSounds = new SoundCollectionSpecifier("deathSounds");

    [DataField]
    public SoundSpecifier HeartSounds = new SoundCollectionSpecifier("heartSounds");

    [DataField]
    public bool CanOtherHearDeathSound;

    public EntityUid AudioStream;
}
