using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BodyPartAppearanceComponent : Component
{
    /// <summary>
    /// Determines whether a body part should change color when the body color changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanChangeColor = true;

    /// <summary>
    /// Determines whether a body part is marking.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsMarking;

    /// <summary>
    /// Determines whether the body part sprite is visible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Visible { get => LayerInfo.Visible; set => LayerInfo.Visible = value; }

    /// <summary>
    /// Sprite the color of this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color { get => LayerInfo.Color; set => LayerInfo.Color = value; }

    /// <summary>
    /// Display layer for this body part.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Enum Layer;

    /// <summary>
    /// Markings of this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<MarkingLayerInfo> MarkingsLayers { get => LayerInfo.MarkingsLayers; set => LayerInfo.MarkingsLayers = value; }

    /// <summary>
    /// Body type of this body part.
    /// </summary>
    [DataField]
    public ProtoId<BodyTypePrototype>? BodyType { get => LayerInfo.BodyType; set => LayerInfo.BodyType = value; }

    /// <summary>
    /// Path to sprite of this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResPath Sprite { get => LayerInfo.Sprite; set => LayerInfo.Sprite = value; }

    /// <summary>
    /// Sex of this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Sex Sex { get => LayerInfo.Sex; set => LayerInfo.Sex = value; }

    /// <summary>
    /// Sprite state of this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string State { get => LayerInfo.State; set => LayerInfo.State = value; }

    public BodyAppearanceLayerInfo LayerInfo = new();
}
