using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedBloodstreamSystem))]
public sealed partial class BloodstreamProviderComponent : Component
{
    /// <summary>
    /// The specific body location this bloodstream is associated with.
    /// Used to categorize or apply bleeding to a particular area.
    /// </summary>
    [DataField]
    public BodyProviderType Location = BodyProviderType.None;

    /// <summary>
    /// Defines the thresholds for mapping the current bleeding amount to a <see cref="Level"/>.
    /// The highest matching threshold determines the current bleeding level.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, BleedingLevel> Thresholds = new()
    {
        {0, BleedingLevel.Zero},
        {1.6f, BleedingLevel.Mild},
        {3.3f, BleedingLevel.Moderate},
        {5.5f, BleedingLevel.Severe},
        {16.6f , BleedingLevel.Mortal},
    };

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// The current amount of bleeding registered by this provider.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Bleeding;

    /// <summary>
    /// The current calculated level of bleeding for this provider, determined by mapping
    /// the <see cref="Bleeding"/> value against the defined <see cref="Thresholds"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public BleedingLevel Level = BleedingLevel.Zero;
}
