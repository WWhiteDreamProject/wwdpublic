using Robust.Shared.Audio;

namespace Content.Server._White.Teleporter;

[RegisterComponent]
public sealed partial class ExperimentalTeleporterComponent : Component
{
    [DataField]
    public int MinTeleportRange = 3;

    [DataField]
    public int MaxTeleportRange = 8;

    [DataField]
    public int EmergencyLength = 4;

    [DataField]
    public List<int> RandomRotations = new() {90, -90};

    [DataField]
    public string? TeleportInEffect = "ExperimentalTeleportInEffect";

    [DataField]
    public string? TeleportOutEffect = "ExperimentalTeleportOutEffect";

    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/_White/Object/Devices/experimentalsyndicateteleport.ogg");
}
