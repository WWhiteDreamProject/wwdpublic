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
}

[Serializable, NetSerializable]
public enum RemoteControlConsoleUiKey : byte
{
    Key
}
