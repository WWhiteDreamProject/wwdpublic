using Robust.Shared.GameStates;

namespace Content.Shared._White.Lockers;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAlertLevelLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Locked = true;

    [DataField, AutoNetworkedField]
    public HashSet<string> LockedAlertLevels = [];

    [DataField, AutoNetworkedField]
    public EntityUid? StationId;
}
