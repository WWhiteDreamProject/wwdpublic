using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

// WWDP EDIT START
[Serializable, NetSerializable]
public sealed partial class AbsorbDNADoAfterFirstEvent : SimpleDoAfterEvent { }     // le extend
[Serializable, NetSerializable]
public sealed partial class AbsorbDNADoAfterSecondEvent : SimpleDoAfterEvent { }    // le stab
[Serializable, NetSerializable]
public sealed partial class AbsorbDNADoAfterThirdEvent : SimpleDoAfterEvent { }     // le suck
// WWDP EDIT END

[Serializable, NetSerializable]
public sealed partial class ChangelingInfectTargetDoAfterEvent : SimpleDoAfterEvent { }
[Serializable, NetSerializable]
public sealed partial class AbsorbBiomatterDoAfterEvent : SimpleDoAfterEvent { }
