using Content.Shared._White.Body.Prototypes;
using Content.Shared._White.Wounds;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Bloodstream.Components;

/// <summary>
/// Handles metabolizing various reagents with given effects.
/// </summary>
[RegisterComponent]
public sealed partial class MetabolizerComponent : Component
{
    /// <summary>
    /// A dictionary defining the different stages of metabolism that this metabolizer will process, ordered by precedence.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MetabolismStagePrototype>, MetabolismStageEntry> Stages = new();

    /// <summary>
    /// A dictionary mapping different wound severities to their corresponding update interval.
    /// </summary>
    [DataField]
    public Dictionary<WoundSeverity, TimeSpan> UpdateIntervalThresholds = new()
    {
        {WoundSeverity.Healthy, TimeSpan.FromSeconds(1f)},
        {WoundSeverity.Minor, TimeSpan.FromSeconds(1.35f)},
        {WoundSeverity.Moderate, TimeSpan.FromSeconds(1.85f)},
        {WoundSeverity.Severe, TimeSpan.FromSeconds(2.3f)},
        {WoundSeverity.Critical, TimeSpan.Zero},
    };

    /// <summary>
    /// A multiplier applied to the base value <see cref="UpdateInterval"/>.
    /// The update frequency is adjusted based on the body's metabolic rate.
    /// </summary>
    [DataField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// A set of identifiers for metabolizer types this metabolizer represents (e.g., "Human", "Slime", "Felinid").
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MetabolizerTypePrototype>> Types = new();

    /// <summary>
    /// The maximum number of unique reagents this metabolizer can process simultaneously per update cycle.
    /// </summary>
    [DataField]
    public int MaxReagentsProcessable = 3;

    /// <summary>
    /// The base interval between reagent metabolism updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The body entity containing this lung, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;

    /// <summary>
    /// Adjusted update interval, calculated by combining the base interval and metabolic rate multipliers.
    /// </summary>
    [ViewVariables]
    public TimeSpan CurrentUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    /// <summary>
    /// The scheduled time for the next reagent metabolism update.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextUpdate;
}

[DataDefinition]
public sealed partial class MetabolismStageEntry
{
    /// <summary>
    /// When true, this solution will be metabolized entirely instead of at a certain rate
    /// </summary>
    [DataField]
    public bool MetabolizeAll;

    /// <summary>
    /// Does this component use a solution on its parent entity (the body) or itself?
    /// </summary>
    /// <remarks>
    /// Most things will use the parent entity (bloodstream).
    /// </remarks>
    [DataField]
    public bool SolutionOnBody = true;

    /// <summary>
    /// The percentage of transferred reagents that actually make it to the next step in metabolism if they don't have explicit metabolites
    /// </summary>
    [DataField]
    public FixedPoint2 TransferEfficacy = 1;

    /// <summary>
    /// Reagents transferred by this metabolizer will transfer at this rate if they don't have a metabolism
    /// </summary>
    [DataField]
    public FixedPoint2 TransferRate = 0.25;

    /// <summary>
    /// From which solution will this metabolizer attempt to metabolize chemicals?
    /// </summary>
    [DataField(required: true)]
    public string SolutionName;

    /// <summary>
    /// A reference to the actual chemical solution.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    /// A reference to the entity that owns the <see cref="Solution"/>.
    /// </summary>
    [ViewVariables]
    public EntityUid? SolutionOwner;
}
