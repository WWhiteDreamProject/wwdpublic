using Content.Server._War.TotalWar.Factions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._War.TotalWar.GameRule;

[RegisterComponent, Access(typeof(TotalWarRuleSystem))]
public sealed partial class TotalWarRuleComponent : Component
{
    [DataField("factions", required: true)]
    public List<ProtoId<WarFactionPrototype>> Factions;

    [DataField]
    public Dictionary<string, int> FactionMembersCounter = new();

    [DataField]
    public HashSet<string> DefeatedFactions = new();
    
    [DataField]
    public bool EndRoundOnOneFaction = false;
}
