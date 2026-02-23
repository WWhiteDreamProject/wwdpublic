using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Body.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodGeneratorComponent : Component
{
    /// <summary>
    /// How much should bleeding be reduced every update interval?
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedReductionAmount = 0.11f;
}
