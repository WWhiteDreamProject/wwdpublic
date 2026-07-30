using Content.Shared.Actions;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._White.PAI.Events;

public sealed partial class ManipulatorToggleActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class ManipulatorMoveEvent : EntityEventArgs
{
    public MapCoordinates Coords { get; }

    public ManipulatorMoveEvent(MapCoordinates coords)
    {
        Coords = coords;
    }
}

[Serializable, NetSerializable]
public sealed class ManipulatorGrabEvent : EntityEventArgs
{
    public NetEntity Ent { get; }

    public ManipulatorGrabEvent(NetEntity ent)
    {
        Ent = ent;
    }
}

[Serializable, NetSerializable]
public sealed class ManipulatorInteractEvent : EntityEventArgs
{
    public ManipulatorInteractEvent()
    {
    }
}
