using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;

namespace Content.Server._White.Event.CapturePoint;

[RegisterComponent]
public sealed partial class CapturePointComponent : Component
{
    [DataField]
    public Color MessageColor = Color.White;

    [DataField]
    public ItemSlot CartridgeSlot = new();

    [DataField]
    public LocId CancelMessage = "capture-point-cancel-message";

    [DataField]
    public LocId EndMessage = "capture-point-end-message";

    [DataField]
    public LocId Sender = "capture-point-sender";

    [DataField]
    public LocId StartMessage = "capture-point-start-message";

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public string CartridgeSlotId = "cartridge_slot";

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(120);
}
