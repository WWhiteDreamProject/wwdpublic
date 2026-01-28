using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using System;

namespace Content.Shared._NC.Netrunning;

[Serializable, NetSerializable]
public sealed partial class CyberdeckQuickhackDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity TargetId;
    public NetEntity ProgramId;

    public CyberdeckQuickhackDoAfterEvent(NetEntity targetId, NetEntity programId)
    {
        TargetId = targetId;
        ProgramId = programId;
    }
}
