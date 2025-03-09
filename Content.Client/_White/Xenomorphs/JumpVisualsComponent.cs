using Robust.Client.Graphics;

namespace Content.Client.Aliens;

/// <summary>
/// The JumpVisualsComponent is used for managing the visual effects of jumps.
/// </summary>
[RegisterComponent]
public sealed partial class JumpVisualsComponent : Component;

/// <summary>
/// JumpLayers is used to specify jump layers.
/// </summary>
public enum JumpLayers : byte
{
    Jumping
}
