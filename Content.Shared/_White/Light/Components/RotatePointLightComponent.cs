using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Light.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RotatePointLightComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Angle Angle;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled = false;
    public bool ClientEnabled = false;
}
