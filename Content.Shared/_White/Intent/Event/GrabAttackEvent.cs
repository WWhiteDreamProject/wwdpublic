using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Intent.Event;

[Serializable, NetSerializable]
public sealed class GrabAttackEvent : AttackEvent
{
    public NetEntity? Target;

    public GrabAttackEvent(NetEntity? target, NetCoordinates coordinates) : base(coordinates)
    {
        Target = target;
    }
}
