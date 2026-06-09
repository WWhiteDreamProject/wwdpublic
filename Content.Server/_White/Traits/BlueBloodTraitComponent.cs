using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Traits;

[RegisterComponent]
public sealed partial class BlueBloodTraitComponent : Component
{
    [DataField]
    public HashSet<ProtoId<MetabolizerTypePrototype>> MetabolizerPrototype = new() { "Arachnid" };

    [DataField]
    public string Blood = "CopperBlood";
}
