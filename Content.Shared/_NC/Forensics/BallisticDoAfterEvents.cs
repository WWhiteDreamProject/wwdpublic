using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Forensics;

[Serializable, NetSerializable]
public sealed partial class BallisticIncisionDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class BallisticExtractionDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class BallisticAnalysisDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class BallisticReassembleDoAfterEvent : SimpleDoAfterEvent;
