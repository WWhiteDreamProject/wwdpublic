using Content.Shared.Actions;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Xenomorphs.Actions;

public sealed partial class TransferPlasmaActionEvent : EntityTargetActionEvent
{
    [DataField]
    public FixedPoint2 Amount = 50;
}
