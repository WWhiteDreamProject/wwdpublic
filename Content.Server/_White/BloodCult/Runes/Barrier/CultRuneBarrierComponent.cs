using Robust.Shared.Prototypes;

namespace Content.Server._White.BloodCult.Runes.Barrier;

[RegisterComponent]
public sealed partial class CultRuneBarrierComponent : Component
{
    [DataField]
    public EntProtoId SpawnPrototype = "BloodCultBarrier";
}
