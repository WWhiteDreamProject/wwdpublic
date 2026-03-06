using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BeaconTeleporterComponent : Component
{
    /// <summary>
    /// Delay for creating the portals.
    /// </summary>
    [DataField]
    public TimeSpan PortalCreationDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound that plays when a portal is created.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? PortalCreateSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg", AudioParams.Default.WithVolume(-2f));

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Beacon;
}
