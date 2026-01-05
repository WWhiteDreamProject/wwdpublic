using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Prototypes;

[Prototype]
public sealed partial class MetabolizerTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    private LocId Name { get; set; }

    [ViewVariables]
    public string LocalizedName => Loc.GetString(Name);
}
