using Content.Shared._White.Body;
using Content.Shared._White.Pain.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedPainfulSystem))]
public sealed partial class PainfulProviderComponent : Component
{
    /// <summary>
    /// The specific body location this pain provider is associated with.
    /// Used to categorize or apply pain to a particular area.
    /// </summary>
    [DataField]
    public BodyProviderType Location = BodyProviderType.None;

    /// <summary>
    /// Threshold values for determining the current pain level of a provider depending on pain.
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

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// The current amount of pain registered by this provider.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Pain = FixedPoint2.Zero;

    /// <summary>
    /// The current level of pain for this provider, determined by mapping
    /// the <see cref="Pain"/> value against the defined <see cref="Thresholds"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public PainLevel Level = PainLevel.Zero;
}
