using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Xenomorphs.Evolution;

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

public sealed partial class OpenEvolutionsActionEvent : InstantActionEvent;
