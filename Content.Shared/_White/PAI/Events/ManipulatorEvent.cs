using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._White.PAI.Events;

public sealed partial class ManipulatorToggleActionEvent : InstantActionEvent;

public sealed partial class ManipulatorGrabToggleActionEvent : InstantActionEvent;

public sealed partial class ManipulatorMoveActionEvent : WorldTargetActionEvent;
