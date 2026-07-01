using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Psionics;

[RegisterComponent]
public sealed partial class CustomPsionicPoolComponent : Component
{
    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> Pool = "RandomPsionicPowerPool";
}
