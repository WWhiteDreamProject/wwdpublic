using Robust.Shared.Audio;

namespace Content.Server._White.BloodCult.Items.VeilShifter;

[RegisterComponent]
public sealed partial class VeilShifterComponent : Component
{
    [DataField]
    public int Charges = 4;

    [DataField]
    public int TeleportDistanceMax = 10;

    [DataField]
    public int TeleportDistanceMin = 5;

    [DataField]
    public Vector2i Offset = Vector2i.One * 2;

    // How many times it will try to find safe location before aborting the operation?
    [DataField]
    public int Attempts = 10;

    [DataField]
    public SoundPathSpecifier TeleportInSound = new("/Audio/_White/Magic/BloodCult/veilin.ogg");

    [DataField]
    public SoundPathSpecifier TeleportOutSound = new("/Audio/_White/Magic/BloodCult/veilout.ogg");

    [DataField]
    public string? TeleportInEffect = "BloodCultTeleportInEffect";

    [DataField]
    public string? TeleportOutEffect = "BloodCultTeleportOutEffect";
}
