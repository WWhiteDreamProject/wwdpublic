using Robust.Shared.Audio;

namespace Content.Server._White.BloodCult.Runes.Teleport;

[RegisterComponent]
public sealed partial class CultRuneTeleportComponent : Component
{
    [DataField]
    public float TeleportGatherRange = 0.65f;

    [DataField]
    public string Name = "";

    [DataField]
    public SoundPathSpecifier TeleportInSound = new("/Audio/_White/Magic/BloodCult/veilin.ogg");

    [DataField]
    public SoundPathSpecifier TeleportOutSound = new("/Audio/_White/Magic/BloodCult/veilout.ogg");
}
