using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BodyAppearanceComponent : Component
{
    [ViewVariables, AutoNetworkedField, NonSerialized]
    public Dictionary<Enum, BodyAppearanceLayerInfo?> Layers = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BodyAppearanceLayerData
{
    /// <summary>
    /// The data for the layer.
    /// </summary>
    [DataField(required: true)]
    public PrototypeLayerData Data;

    /// <summary>
    /// The "body type" of the layer.
    /// </summary>
    [DataField]
    public ProtoId<BodyTypePrototype>? BodyType;

    /// <summary>
    /// The color of the layer
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// The "sex" of this layer.
    /// </summary>
    [DataField]
    public Sex Sex = Sex.Unsexed;
}

[Serializable, NetSerializable]
public enum BodyAppearanceLayer
{
    Head,
    Chest,
    Groin,
    LeftArm,
    RightArm,
    LeftHand,
    RightHand,
    LeftLeg,
    RightLeg,
    LeftFoot,
    RightFoot,
    Tail,
    Wings,
    Eyes
}
