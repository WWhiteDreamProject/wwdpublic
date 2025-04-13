using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;


/// <summary>
/// Indicates this entity is currently held inside of a station AI core.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiHeldComponent : Component
{
    // WD edit start
    [DataField]
    public SoundPathSpecifier CoreBoltsDisabled = new("/Audio/Machines/boltsdown.ogg");

    [DataField]
    public SoundPathSpecifier CoreBoltsEnabled = new("/Audio/Machines/boltsup.ogg");
    // WD edit end
};
