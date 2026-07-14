using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Actions.Events;

public sealed partial class PsionicHookPowerActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class PsionicUncuffDoAfterEvent : SimpleDoAfterEvent;
