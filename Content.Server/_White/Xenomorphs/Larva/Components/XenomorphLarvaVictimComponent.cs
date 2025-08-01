using Content.Shared._White.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Xenomorphs.Larva.Components;

[RegisterComponent]
public sealed partial class XenomorphLarvaVictimComponent : Component
{
    [ViewVariables]
    public ProtoId<InfectionIconPrototype>? InfectedIcon;
}
