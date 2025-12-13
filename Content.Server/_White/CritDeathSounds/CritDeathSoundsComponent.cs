using Robust.Shared.Audio;

namespace Content.Server._White.CritDeathSounds;

[RegisterComponent]
public sealed partial class CritDeathSoundsComponent : Component
{
    [DataField]
    public SoundSpecifier DeathSounds = new SoundCollectionSpecifier("deathSounds");

    [DataField]
    public SoundSpecifier HeartSounds = new SoundCollectionSpecifier("heartSounds");

    [DataField]
    public bool CanOtherHearDeathSound;
}
