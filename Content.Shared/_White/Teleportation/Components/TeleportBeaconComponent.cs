using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleportBeaconComponent : Component
{
    /// <summary>
    /// The sound played when a beacon is linked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? LinkSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");
}
