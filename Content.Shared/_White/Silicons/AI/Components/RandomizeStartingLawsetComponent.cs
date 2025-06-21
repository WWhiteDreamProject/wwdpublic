using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;


namespace Content.Shared._White.Silicons.AI.Components;


[RegisterComponent]
public sealed partial class RandomizeStartingLawsetComponent : Component
{
    /// <summary>
    /// List of lawset IDs to choose from
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SiliconLawsetPrototype>> Lawsets = [];
}
