using Content.Shared._White.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Organs.Metabolizer;

/// <summary>
/// Handles metabolizing various reagents with given effects.
/// </summary>
[RegisterComponent]
public sealed partial class MetabolizerComponent : Component
{
    /// <summary>
    /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on curent organ health.
    /// </summary>
    [DataField]
    public float UpdateIntervalHealthMultiplier = 1f;

    /// <summary>
    /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on metabolic rate multiplier.
    /// </summary>
    [DataField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// List of metabolizer types that this organ is. Ex. Human, Slime, Felinid, w/e.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MetabolizerTypePrototype>> Types = new();

    /// <summary>
    /// A list of metabolism stages that this metabolizer will act on, in order of precedence.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MetabolismStagePrototype>, MetabolismStageEntry> Stages = new();

    /// <summary>
    /// How many reagents can this metabolizer process at once?
    /// Used to nerf 'stacked poisons' where having 5+ different poisons in a syringe, even at low
    /// quantity, would be much better than just one poison acting.
    /// </summary>
    [DataField]
    public int MaxReagentsProcessable = 3;

    /// <summary>
    /// How often to metabolize reagents.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public bool Enable;

    [ViewVariables]
    public EntityUid? Body;

    /// <summary>
    /// The next time that reagents will be metabolized.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Adjusted update interval based off of the multiplier value.
    /// </summary>
    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier * UpdateIntervalHealthMultiplier;
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

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    [ViewVariables]
    public EntityUid? SolutionOwner;
}
