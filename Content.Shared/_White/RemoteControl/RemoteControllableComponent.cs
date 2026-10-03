using Robust.Shared.Serialization;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._White.RemoteControl;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControllableComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Name = string.Empty;

    [DataField, AutoNetworkedField]
    public Angle AngleTolerance = Math.PI / 720; // 0.25 degrees

    [DataField, AutoNetworkedField]
    public Angle RotationSpeed = Math.PI / 3;

    [DataField, AutoNetworkedField]
    public float AimpointTolerane = 0.5f;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentConsole;

    // TODO: move the camera display to a separate component, similiar
    //       to RadarConsoleComp. This would let us separate variables
    //       that are only relevant to camera display into another component, while
    //       allowing us to ditch this variable. Otherwise we'll have radar related
    //       variables in RadarConsoleComponent and camera related vars in this component,
    //       which doesn't make much sense.
    //
    //       This would require making this new component work with CCTV cameras system,
    //       since it would make even less sense to make some "RemoteCameraComponent"
    //       that would not be used for actual cameras.
    [DataField, AutoNetworkedField]
    public RemoteControlVisualMode Display = RemoteControlVisualMode.Radar;

}

public enum RemoteControlVisualMode
{
    None, Radar, Camera, Both
}

[Serializable, NetSerializable]
public enum RemoteControlConsoleUiKey : byte
{
    Key
}

