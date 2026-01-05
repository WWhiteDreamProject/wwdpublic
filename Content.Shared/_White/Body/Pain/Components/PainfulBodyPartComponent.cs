using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Body.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PainfulBodyPartComponent : Component
{
    /// <summary>
    /// The maximum amount of pain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaximumPain = 100;

    /// <summary>
    /// The current amount of pain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Pain = FixedPoint2.Zero;

    /// <summary>
    /// The current level of pain in this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PainLevel PainLevel = PainLevel.Zero;

    /// <summary>
    /// Threshold values for determining the current pain level of a body part depending on pain.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, PainLevel> Thresholds = new()
    {
        {0, PainLevel.Zero},
        {10, PainLevel.Mild},
        {25, PainLevel.Moderate},
        {50, PainLevel.Severe},
        {80, PainLevel.Excruciating},
        {95, PainLevel.Mortal},
    };
}
