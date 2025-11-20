using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.BloodCult.Runes;

[Serializable, NetSerializable]
public enum RuneDrawerBuiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RuneDrawerMenuState(List<ProtoId<BloodCultRunePrototype>> availableRunes) : BoundUserInterfaceState
{
    public List<ProtoId<BloodCultRunePrototype>> AvailableRunes { get; private set; } = availableRunes;
}

[Serializable, NetSerializable]
public sealed class RuneDrawerSelectedMessage(ProtoId<BloodCultRunePrototype> selectedRune) : BoundUserInterfaceMessage
{
    public ProtoId<BloodCultRunePrototype> SelectedRune { get; private set; } = selectedRune;
}

[Serializable, NetSerializable]
public sealed partial class ApocalypseRuneDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RendingRuneDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RuneEraseDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class DrawRuneDoAfter : SimpleDoAfterEvent
{
    public ProtoId<BloodCultRunePrototype> Rune;
    public SoundSpecifier EndDrawingSound;
}
