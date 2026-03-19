using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware;

[Serializable, NetSerializable]
public sealed partial class AutodocInstallDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity Implant;
    public CyberwareSlot Slot;

    public AutodocInstallDoAfterEvent(NetEntity implant, CyberwareSlot slot)
    {
        Implant = implant;
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public sealed partial class AutodocRemoveDoAfterEvent : SimpleDoAfterEvent
{
    public CyberwareSlot Slot;

    public AutodocRemoveDoAfterEvent(CyberwareSlot slot)
    {
        Slot = slot;
    }
}