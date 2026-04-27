using Robust.Shared.Serialization;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._White.RemoteControl;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControlConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTurret;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Shooting;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Angle? CurrentAimDirection;
}
