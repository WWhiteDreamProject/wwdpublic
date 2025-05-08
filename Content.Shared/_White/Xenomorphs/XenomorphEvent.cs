using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Xenomorphs;

[Serializable, NetSerializable]
public sealed partial class XenomorphEvolutionDoAfterEvent : DoAfterEvent
{
    [DataField]
    public EntProtoId Choice;

    public XenomorphEvolutionDoAfterEvent(EntProtoId choice)
    {
        Choice = choice;
    }

    public override DoAfterEvent Clone() => this;
}

public sealed partial class TransferPlasmaActionEvent : EntityTargetActionEvent
{
    [DataField]
    public FixedPoint2 Amount = 50;
}

public sealed partial class OpenEvolutionsActionEvent : InstantActionEvent;

public sealed partial class TailLashActionEvent : WorldTargetActionEvent;

public sealed partial class AcidActionEvent : EntityTargetActionEvent;

public sealed partial class AfterXenomorphEvolutionEvent(EntityUid evolvedInto, EntityUid evolvedFrom, string? userName)
{
    public EntityUid EvolvedInto = evolvedInto;
    public EntityUid EvolvedFrom = evolvedFrom;
    public string? UserName = userName;
}
