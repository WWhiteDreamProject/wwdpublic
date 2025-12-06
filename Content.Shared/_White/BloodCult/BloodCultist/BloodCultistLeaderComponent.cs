using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.BloodCult.BloodCultist;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultistLeaderComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultLeader";
}
