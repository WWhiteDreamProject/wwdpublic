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
public sealed partial class BodyAppearanceLayerInfo
{
    [DataField]
    public ProtoId<BodyTypePrototype>? BodyType;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public List<MarkingLayerInfo> MarkingsLayers = new();

    [DataField]
    public ResPath Sprite;

    [DataField]
    public Sex Sex = Sex.Unsexed;

    [DataField]
    public string State = string.Empty;

    [DataField]
    public bool Visible = true;
}
