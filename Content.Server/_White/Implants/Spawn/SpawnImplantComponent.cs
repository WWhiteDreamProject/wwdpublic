using Robust.Shared.Audio;

namespace Content.Server._White.Implants.Spawn;

[RegisterComponent]
public sealed partial class SpawnImplantComponent : Component
{
    [DataField(required: true)]
    public string SpawnId = string.Empty;

    [DataField]
    public SoundSpecifier SoundOnSpawn = new SoundPathSpecifier("/Audio/Weapons/ebladeon.ogg");
}
