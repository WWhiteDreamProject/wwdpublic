using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._White.Lighting.Shaders;

/// <summary>
/// This is used for LightOverlay
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LightingOverlayComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool? Enabled;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Sprite = new Texture(new ResPath("_White/Effects/LightMasks/lightmask_lamp.png"));

    [DataField, AutoNetworkedField]
    public float Offsetx = -0.5f;

    [DataField, AutoNetworkedField]
    public float Offsety = 0.5f;

    [DataField, AutoNetworkedField]
    public Color? Color;
}
