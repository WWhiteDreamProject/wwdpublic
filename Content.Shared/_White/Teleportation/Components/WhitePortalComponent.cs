using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Shared._White.Teleportation.Components;

[RegisterComponent]
public sealed partial class WhitePortalComponent : Component
{
    /// <summary>
    /// The sound that plays when entering the portal.
    /// </summary>
    [DataField]
    public SoundSpecifier? EnteringSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    /// The maximum distance you can move the portal if the specified coordinates are not available.
    /// </summary>
    [DataField]
    public float MaxRandomDistance = 1000f;

    [ViewVariables]
    public EntityCoordinates? Coordinates;
}
