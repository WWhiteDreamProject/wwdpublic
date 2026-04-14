using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.Events;

[Serializable, NetSerializable]
public sealed class CyberwareDodgeEvent : EntityEventArgs
{
    public NetEntity Target;

    public CyberwareDodgeEvent(NetEntity target)
    {
        Target = target;
    }
}
