using Content.Shared._White.Body.Components;
using Content.Shared.Alert;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class PainThresholdsComponent : Component
{
    /// <summary>
    /// The health alert that should be displayed for player controlled entities.
    /// Used for alternate health alerts (silicons, for example)
    /// </summary>
    [DataField]
    public Dictionary<MobState, ProtoId<AlertPrototype>> StateAlertDict = new()
    {
        {MobState.Alive, "HumanHealth"},
        {MobState.Critical, "HumanCrit"},
        {MobState.SoftCritical, "HumanCrit"},
        {MobState.Dead, "HumanDead"},
    };

    [DataField]
    public ProtoId<AlertCategoryPrototype> AlertCategory = "Health";

    [DataField]
    public ProtoId<AlertPrototype> BodyStatusAlert = "BodyStatus";

    /// <summary>
    /// The mob state to apply depending on the amount of pain. The highest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, MobState> MobStateThresholds;

    /// <summary>
    /// Effects applied to the entity at the appropriate level of pain.
    /// </summary>
    [DataField(serverOnly: true)] // TODO: Remove serverOnly when we move EntityEffect to shared
    public Dictionary<PainLevel, List<EntityEffect>> PainEffects = new();

    /// <summary>
    /// The pain level to apply depending on the amount of pain. the highest threshold is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, PainLevel> PainLevelThresholds;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<BodyPartType, PainLevel> PainStatus = new()
    {
        { BodyPartType.Head, PainLevel.Zero },
        { BodyPartType.Chest, PainLevel.Zero },
        { BodyPartType.Groin, PainLevel.Zero },
        { BodyPartType.LeftArm, PainLevel.Zero },
        { BodyPartType.LeftHand, PainLevel.Zero },
        { BodyPartType.RightArm, PainLevel.Zero },
        { BodyPartType.RightHand, PainLevel.Zero },
        { BodyPartType.LeftLeg, PainLevel.Zero },
        { BodyPartType.LeftFoot, PainLevel.Zero },
        { BodyPartType.RightLeg, PainLevel.Zero },
        { BodyPartType.RightFoot, PainLevel.Zero },
    };

    [ViewVariables, AutoNetworkedField]
    public MobState CurrentMobStateThreshold = MobState.Alive;

    [ViewVariables, AutoNetworkedField]
    public PainLevel CurrentPainLevelThreshold = PainLevel.Zero;
}

[Serializable, NetSerializable]
public enum PainLevel : byte
{
    None,
    Zero,
    Mild,
    Moderate,
    Severe,
    Excruciating,
    Mortal,
}

[NetSerializable, Serializable]
public enum BodyStatusVisualLayers : byte
{
    Head,
    Chest,
    Groin,
    LeftArm,
    LeftHand,
    RightArm,
    RightHand,
    LeftLeg,
    LeftFoot,
    RightLeg,
    RightFoot,
}
