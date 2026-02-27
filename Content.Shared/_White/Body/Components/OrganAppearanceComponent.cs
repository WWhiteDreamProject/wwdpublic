using Robust.Shared.GameStates;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class OrganAppearanceComponent : Component
{
    /// <summary>
    /// The layer on the entity that this contributes to
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The data for the layer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BodyAppearanceLayerData Data = new();
}
