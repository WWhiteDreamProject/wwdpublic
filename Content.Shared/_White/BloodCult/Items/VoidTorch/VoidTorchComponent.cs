using Robust.Shared.Audio;

namespace Content.Shared._White.BloodCult.Items.VoidTorch;

[RegisterComponent]
public sealed partial class VoidTorchComponent : Component
{
    [DataField]
    public int Charges = 5;

    [DataField]
    public SoundPathSpecifier TeleportSound = new("/Audio/_White/Magic/BloodCult/veilin.ogg");

    [DataField]
    public string TurnOnLightBehaviour = "turn_on";

    [DataField]
    public string TurnOffLightBehaviour = "fade_out";

    public EntityUid? TargetItem;
}
