using Content.Shared._White.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Xenomorphs.Infection.Components;

[RegisterComponent]
public sealed partial class XenomorphInfectedComponent : Component
{
    [ViewVariables]
    public Dictionary<int, ProtoId<InfectionIconPrototype>> InfectedIcons = new();

    [ViewVariables]
    public EntityUid Infection;

    [ViewVariables]
    public int GrowthStage;
}
