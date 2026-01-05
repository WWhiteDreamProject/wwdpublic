using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class BloodstreamComponent : Component
{
    /// <summary>
    /// The blood type that flows in the entity.
    /// </summary>
    [DataField(customTypeSerializer: typeof(BloodGroupSerializer)), AutoNetworkedField]
    public BloodGroup BloodGroup = new BloodGroup();

    /// <summary>
    /// From which solution will this metabolizer attempt to metabolize chemicals for a given stage?
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MetabolismStagePrototype>, MetabolismStagesEntry> Stages = new()
    {
        ["Respiration"] = new()
        {
            NextStage = "Absorption"
        },
        ["Digestion"] = new()
        {
            NextStage = "Absorption"
        },
        ["Absorption"] = new()
        {
            NextStage = "Excretion"
        },
        ["Excretion"] = new()
    };

    /// <summary>
    /// How much is this entity currently bleeding?
    /// Higher numbers mean more blood lost every tick.
    ///
    /// Goes down slowly over time, and items like bandages
    /// or clotting reagents can lower bleeding.
    /// </summary>
    /// <remarks>
    /// This generally corresponds to the amount of damage and can't go above 100.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Bleeding = FixedPoint2.Zero;

    /// <summary>
    /// How much blood needs to be in the temporary solution in order to create a puddle?
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedPuddleThreshold = 1.0f;

    /// <summary>
    /// Max volume of internal blood storage,
    /// and starting level of blood.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodMaxVolume = FixedPoint2.New(300);

    /// <summary>
    /// Max volume of internal chemical solution storage
    /// </summary>
    [DataField]
    public FixedPoint2 ChemicalMaxVolume = FixedPoint2.New(250);

    /// <summary>
    /// How high can <see cref="Bleeding"/> go?
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaximumBleeding = 10;

    /// <summary>
    /// Maximum mass coefficient regulating bloodstream volume.
    /// </summary>
    [DataField]
    public float MaxMassAdjust = 3f;

    /// <summary>
    /// Minimum mass coefficient regulating bloodstream volume.
    /// </summary>
    [DataField]
    public float MinMassAdjust = 1 / 3f;

    /// <summary>
    /// The power to which the outcome of the mass contest will be risen.
    /// </summary>
    [DataField]
    public float PowerMassAdjust;

    /// <summary>
    /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on metabolic rate multiplier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// Alert to show when bleeding.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BleedingAlert = "Bleed";

    /// <summary>
    /// The sound to be played when some damage actually heals bleeding rather than starting it.
    /// </summary>
    [DataField]
    public SoundSpecifier BloodHealedSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    /// <summary>
    /// Defines which reagents are considered as 'blood'.
    /// </summary>
    /// <remarks>
    /// Slime-people might use slime as their blood or something like that.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> BloodReagent = "Blood";

    /// <summary>
    /// The sound to be played when a weapon instantly deals blood loss damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier InstantBloodSound = new SoundCollectionSpecifier("blood");

    /// <summary>
    /// Name/Key that <see cref="BloodSolution"/> is indexed by.
    /// </summary>
    [DataField]
    public string BloodSolutionName = "bloodstream";

    /// <summary>
    /// Name/Key that <see cref="TemporarySolution"/> is indexed by.
    /// </summary>
    [DataField]
    public string BloodTemporarySolutionName = "bloodstreamTemporary";

    /// <summary>
    /// The interval at which this component updates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Multiplier applied to <see cref="BloodMaxVolume"/> for adjusting based on body mass index.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public double BloodMaxVolumeMultiplier = 1d;

    /// <summary>
    /// Internal solution for blood storage.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? BloodSolution;

    /// <summary>
    /// Temporary blood solution.
    /// When blood is lost, it goes to this solution, and when this
    /// solution hits a certain cap, the blood is actually spilled as a puddle.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? TemporarySolution;

    /// <summary>
    /// Adjusted blood max volume based off of the multiplier value.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 CurrentBloodMaxVolume => BloodMaxVolume * BloodMaxVolumeMultiplier;

    [ViewVariables]
    public float SaturationLevel = 1f;

    /// <summary>
    /// Caches the blood data of an entity.
    /// This is modified by DNA on init so it's not savable.
    /// </summary>
    [ViewVariables]
    public List<ReagentData>? BloodData;

    /// <summary>
    /// Adjusted update interval based off of the multiplier value.
    /// </summary>
    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    /// <summary>
    /// The next time that blood level will be updated and bloodloss damage dealt.
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
