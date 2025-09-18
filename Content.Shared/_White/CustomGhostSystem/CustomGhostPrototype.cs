using System.Numerics;
using Content.Shared.Ghost;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.CustomGhostSystem;

[Prototype("customGhost")]
public sealed class CustomGhostPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public string? Ckey;

    [DataField("proto", required: true)]
    public EntProtoId<GhostComponent> GhostEntityPrototype = default!;

    [DataField]
    public Dictionary<string, float>? PlaytimeHours;

    [DataField("name")]
    public string? Name;

    [DataField("description")]
    public string? Description;
}
