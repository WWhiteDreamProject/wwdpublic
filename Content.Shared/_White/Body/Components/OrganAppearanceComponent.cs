using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class OrganAppearanceComponent : Component
{
    /// <summary>
    /// Determines whether an organ should change color when the organ color changes
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanChangeColor = true;

    /// <summary>
    /// Sprite the color of this organ.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color { get => LayerInfo.Color; set => LayerInfo.Color = value; }

    /// <summary>
    /// Display layer for this organ.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Enum Layer;

    /// <summary>
    /// Markings of this organ.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<MarkingLayerInfo> MarkingsLayers { get => LayerInfo.MarkingsLayers; set => LayerInfo.MarkingsLayers = value; }

    /// <summary>
    /// Path to sprite of this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResPath Sprite { get => LayerInfo.Sprite; set => LayerInfo.Sprite = value; }

    /// <summary>
    /// Sprite state of this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string State { get => LayerInfo.State; set => LayerInfo.State = value; }

    public BodyAppearanceLayerInfo LayerInfo = new();
}
