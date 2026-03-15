using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Actions;

// Эти классы необходимы для работы гудков и сирен в прототипах, 
// так как Robust требует наличия C# класса для каждого ActionEvent.

[Serializable, NetSerializable]
public sealed partial class HornActionEvent : InstantActionEvent {}

[Serializable, NetSerializable]
public sealed partial class SirenActionEvent : InstantActionEvent {}
