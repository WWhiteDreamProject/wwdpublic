using Content.Server.Maps;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._War.TotalWar.Factions;

[Prototype("warFaction")]
public sealed class WarFactionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name", required: true)]
    public string Name { get; } = default!;

    [DataField("maps", required: true)]
    public List<ProtoId<GameMapPoolPrototype>> Maps { get; set; } = new();

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> NpcFactions = new();
}
