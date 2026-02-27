using Content.Shared._White.Pain.Systems;
using Content.Shared.Alert;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedPainfulSystem))]
public sealed partial class PainThresholdsComponent : Component
{
    /// <summary>
    /// A mapping of entity states to the <see cref="AlertPrototype"/> IDs that should be displayed for player-controlled entities
    /// when they are in that state. This allows for customized health alerts based on mob state.
    /// For example, silicon entities might have different alerts than human entities.
    /// </summary>
    [DataField]
    public Dictionary<MobState, ProtoId<AlertPrototype>> StateAlerts = new()
    {
        {MobState.Alive, "HumanHealth"},
        {MobState.Critical, "HumanCrit"},
        {MobState.SoftCritical, "HumanCrit"},
        {MobState.Dead, "HumanDead"},
    };

    /// <summary>
    /// The <see cref="AlertCategoryPrototype"/> ID used for grouping health-related alerts.
    /// </summary>
    [DataField]
    public ProtoId<AlertCategoryPrototype> AlertCategory = "Health";

    /// <summary>
    /// Defines the thresholds for mapping an entity's current pain value to a <see cref="MobState"/>.
    /// The highest matching threshold determines the <see cref="MobState"/>.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, MobState> MobStateThresholds = new()
    {
        {0, MobState.Alive},
        {200, MobState.Critical},
        {500, MobState.Dead},
    };

    /// <summary>
    /// A dictionary mapping specific <see cref="PainLevel"/> values to a list of <see cref="EntityEffect"/>s that should be applied
    /// to the entity when its pain reaches or exceeds that level.
    /// </summary>
    [DataField(serverOnly: true)] // TODO: Remove serverOnly when we move EntityEffect to shared
    public Dictionary<PainLevel, List<EntityEffect>> PainEffects = new();

    /// <summary>
    /// Defines the thresholds for mapping an entity's current pain value to a specific <see cref="PainLevel"/>.
    /// The highest matching threshold determines the <see cref="PainLevel"/>.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, PainLevel> PainLevelThresholds = new()
    {
        {0, PainLevel.Zero},
        {25, PainLevel.Mild},
        {70, PainLevel.Moderate},
        {130, PainLevel.Severe},
        {200, PainLevel.Excruciating},
        {350, PainLevel.Mortal},
    };

    /// <summary>
    /// The <see cref="MobState"/> determined by the highest matching threshold in <see cref="MobStateThresholds"/> based on the entity's current pain.
    /// This is automatically updated when pain changes.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public MobState MobState = MobState.Alive;

    /// <summary>
    /// The <see cref="PainLevel"/> determined by the highest matching threshold in <see cref="PainLevelThresholds"/> based on the entity's current pain.
    /// This is automatically updated when pain changes.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public PainLevel PainLevel = PainLevel.Zero;
}
