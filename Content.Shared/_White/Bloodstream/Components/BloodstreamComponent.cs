using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedBloodstreamSystem), typeof(SharedMetabolizerSystem))]
public sealed partial class BloodstreamComponent : Component
{
    /// <summary>
    /// The specific blood type (e.g., A+, O-) that flows in this entity's bloodstream.
    /// </summary>
    [DataField(customTypeSerializer: typeof(BloodGroupSerializer)), AutoNetworkedField]
    public BloodGroup Group = new BloodGroup();

    /// <summary>
    /// Defines the <see cref="AlertPrototype"/> IDs to be displayed for different bleeding levels.
    /// </summary>
    [DataField]
    public Dictionary<BleedingLevel, ProtoId<AlertPrototype>> Alerts = new()
    {
        {BleedingLevel.Mild, "Bleed"},
        {BleedingLevel.Moderate, "Bleed"},
        {BleedingLevel.Severe, "Bleed"},
        {BleedingLevel.Mortal, "Bleed"},
    };

    /// <summary>
    /// Defines the metabolic pathway for chemicals within the bloodstream.
    /// It maps <see cref="MetabolismStagePrototype"/> IDs to their processing entries.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MetabolismStagePrototype>, MetabolismStagesEntry> Stages = new()
    {
        ["Respiration"] = new()
        {
            NextStage = "Absorption",
        },
        ["Digestion"] = new()
        {
            NextStage = "Absorption",
        },
        ["Absorption"] = new()
        {
            NextStage = "Excretion",
        },
        ["Excretion"] = new(),
    };

    /// <summary>
    /// The minimum amount of blood that needs to accumulate in the temporary solution
    /// before it can form a visible blood puddle on the ground.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedPuddleThreshold = 1.0f;

    /// <summary>
    /// The maximum total volume of blood that the entity's bloodstream can hold.
    /// This also serves as the initial blood level upon component creation.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxVolume = FixedPoint2.New(550);

    /// <summary>
    /// The maximum volume for the chemical solution storage.
    /// This is distinct from blood volume and likely pertains to other chemical solutions handled by the component.
    /// </summary>
    [DataField]
    public FixedPoint2 ChemicalMaxVolume = FixedPoint2.New(250);

    /// <summary>
    /// The current blood level, represented as a fraction or percentage (e.g., 1.0f for full, 0.5f for half).
    /// This is a normalized value and may not directly reflect the absolute volume.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Level = 1f;

    /// <summary>
    /// A coefficient used to adjust the bloodstream volume based on the entity's body mass.
    /// It defines the upper limit for how much bloodstream volume can be increased relative to mass.
    /// </summary>
    [DataField]
    public float MaxMassAdjust = 3f;

    /// <summary>
    /// A coefficient used to adjust the bloodstream volume based on the entity's body mass.
    /// It defines the lower limit for how much bloodstream volume can be decreased relative to mass.
    /// </summary>
    [DataField]
    public float MinMassAdjust = 1 / 3f;

    /// <summary>
    /// The exponent to which the outcome of a mass-based contest will be raised.
    /// This likely influences how body mass affects bloodstream volume in a non-linear fashion.
    /// </summary>
    [DataField]
    public float PowerMassAdjust;

    /// <summary>
    /// The current metabolic rate.
    /// </summary>
    [DataField]
    public float MetabolicRate = 1f;

    /// <summary>
    /// A multiplier applied to the base value <see cref="UpdateInterval"/>.
    /// The update frequency is adjusted based on the entity's metabolic rate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// The <see cref="AlertCategoryPrototype"/> ID used for grouping bleed-related alerts.
    /// </summary>
    [DataField]
    public ProtoId<AlertCategoryPrototype> AlertCategory = "Bleed";

    /// <summary>
    /// Defines which specific reagent prototypes are considered as 'blood' for this entity.
    /// </summary>
    /// <remarks>
    /// This allows for custom blood types (e.g., slime-people might use slime as their blood).
    /// </remarks>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "Blood";

    /// <summary>
    /// The key name used to identify the main blood solution.
    /// </summary>
    [DataField]
    public string SolutionName = "bloodstream";

    /// <summary>
    /// The key name used to identify the temporary blood solution.
    /// This solution holds blood that has been lost but not yet fully spilled as puddles.
    /// </summary>
    [DataField]
    public string TemporarySolutionName = "temporaryBloodstream";

    /// <summary>
    /// The base interval at which this component's update logic (e.g., blood loss, metabolism) is executed.
    /// This can be modified by <see cref="UpdateIntervalMultiplier"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Defines the thresholds for mapping the current raw bleeding amount to a <see cref="BleedingLevel"/>.
    /// The highest matching threshold determines the current bleeding level.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, BleedingLevel> BleedingThresholds = new()
    {
        {0, BleedingLevel.Zero},
        {1.6f, BleedingLevel.Mild},
        {3.3f, BleedingLevel.Moderate},
        {5.5f, BleedingLevel.Severe},
        {16.6f , BleedingLevel.Mortal},
    };

    /// <summary>
    /// The current calculated level of bleeding (e.g., Mild, Moderate, Severe),
    /// determined by mapping the <see cref="Bleeding"/> value against the defined <see cref="BleedingThresholds"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public BleedingLevel BleedingLevel = BleedingLevel.Zero;

    /// <summary>
    /// A multiplier applied to <see cref="MaxVolume"/> to adjust the total blood volume based on the entity's body mass.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public double MassAdjustMultiplier = 1d;

    /// <summary>
    /// Reference to the entity's main blood solution.
    /// This is typically where the actual blood reagent is stored.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    /// A temporary solution used to buffer blood loss before it's fully spilled as puddles.
    /// Blood is temporarily stored here when lost, and if this solution exceeds
    /// <see cref="BleedPuddleThreshold"/>, the excess spills.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? TemporarySolution;

    /// <summary>
    /// The current total amount of blood in the entity's bloodstream,
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Amount = FixedPoint2.Zero;

    /// <summary>
    /// The current size of active bleeding, indicating how much blood is being lost per update.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Bleeding = FixedPoint2.Zero;

    /// <summary>
    /// Calculates the current maximum blood volume, adjusted by the <see cref="MassAdjustMultiplier"/>.
    /// This provides the effective maximum blood capacity based on the entity's size and potentially other factors.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 CurrentMaxVolume => MaxVolume * MassAdjustMultiplier;

    /// <summary>
    /// Caches the raw reagent data associated with the entity's blood.
    /// This data typically contains DNA and blood type information.
    /// </summary>
    [ViewVariables]
    public List<ReagentData>? Data;

    /// <summary>
    /// Calculates the actual update interval for this component, adjusted by the <see cref="UpdateIntervalMultiplier"/>.
    /// </summary>
    [ViewVariables]
    public TimeSpan CurrentUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    /// <summary>
    /// The scheduled time for the next update cycle of this component.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

[DataDefinition]
public sealed partial class MetabolismStagesEntry
{
    /// <summary>
    /// Reagents without a metabolism for the current stage will be transferred to this stage.
    /// </summary>
    [DataField]
    public ProtoId<MetabolismStagePrototype>? NextStage;

    [ViewVariables]
    public List<MetabolismStageEntry> Stages = new ();
}
