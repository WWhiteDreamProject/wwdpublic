using Robust.Shared.Prototypes;

namespace Content.Server._War.TotalWar.Factions.Members;

[RegisterComponent]
public sealed partial class WarFactionMemberComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WarFactionPrototype> Faction;
}
