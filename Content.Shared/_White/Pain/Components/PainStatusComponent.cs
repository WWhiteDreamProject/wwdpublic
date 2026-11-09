using Content.Shared._White.Body;
using Content.Shared._White.Pain.Systems;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPainfulSystem))]
public sealed partial class PainStatusComponent : Component
{
    /// <summary>
    /// Represents the current pain level for different body locations on an entity.
    /// This dictionary maps each <see cref="BodyProviderType"/> to its associated <see cref="PainLevel"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<BodyProviderType, PainLevel> PainStatus = new()
    {
        { BodyProviderType.Head, PainLevel.Zero },
        { BodyProviderType.Chest, PainLevel.Zero },
        { BodyProviderType.Groin, PainLevel.Zero },
        { BodyProviderType.LeftArm, PainLevel.Zero },
        { BodyProviderType.LeftHand, PainLevel.Zero },
        { BodyProviderType.RightArm, PainLevel.Zero },
        { BodyProviderType.RightHand, PainLevel.Zero },
        { BodyProviderType.LeftLeg, PainLevel.Zero },
        { BodyProviderType.LeftFoot, PainLevel.Zero },
        { BodyProviderType.RightLeg, PainLevel.Zero },
        { BodyProviderType.RightFoot, PainLevel.Zero },
    };

    /// <summary>
    /// The prototype ID for an alert to be displayed pain status.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "PainStatus";

    /// <summary>
    /// Maps body provider types to their corresponding visual layers for rendering pain status alert.
    /// </summary>
    [DataField]
    public Dictionary<BodyProviderType, PainStatusVisualLayers> Layers = new()
    {
        { BodyProviderType.Head, PainStatusVisualLayers.Head },
        { BodyProviderType.Chest, PainStatusVisualLayers.Chest },
        { BodyProviderType.Groin, PainStatusVisualLayers.Groin },
        { BodyProviderType.LeftArm, PainStatusVisualLayers.LeftArm },
        { BodyProviderType.LeftHand, PainStatusVisualLayers.LeftHand },
        { BodyProviderType.RightArm, PainStatusVisualLayers.RightArm },
        { BodyProviderType.RightHand, PainStatusVisualLayers.RightHand },
        { BodyProviderType.LeftLeg, PainStatusVisualLayers.LeftLeg },
        { BodyProviderType.LeftFoot, PainStatusVisualLayers.LeftFoot },
        { BodyProviderType.RightLeg, PainStatusVisualLayers.RightLeg },
        { BodyProviderType.RightFoot, PainStatusVisualLayers.RightFoot },
    };
}

[NetSerializable, Serializable]
public enum PainStatusVisualLayers : byte
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

public sealed partial class CheckPainStatusAlertEvent : BaseAlertEvent;
