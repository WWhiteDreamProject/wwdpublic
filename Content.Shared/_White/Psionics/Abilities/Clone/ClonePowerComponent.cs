using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Abilities.Psionics;

[RegisterComponent]
public sealed partial class ClonePowerComponent : Component
{
    [DataField]
    public SoundSpecifier CloneSound = new SoundPathSpecifier("/Audio/_White/Object/Devices/experimentalsyndicateteleport.ogg");

    [DataField]
    public EntProtoId? CloneEffect = "ExperimentalTeleportInEffect";

    [ViewVariables]
    public EntityUid? CloneUid;
}
