using Robust.Shared.GameStates;

namespace Content.Shared._White.VisionLimit.Components;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class ClothingLimitVisionComponent: Component
{
    /// <summary>
    /// Limits wearer's vision to roughly Radius tiles
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Radius; // in tiles, 0 = no limit
}
