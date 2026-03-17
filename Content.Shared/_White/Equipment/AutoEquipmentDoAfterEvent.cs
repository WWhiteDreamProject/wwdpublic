using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Equipment.Events;

[Serializable, NetSerializable]
public sealed partial class AutoEquipmentDoAfterEvent : SimpleDoAfterEvent
{
}
