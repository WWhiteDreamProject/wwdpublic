using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Light.Components;

[RegisterComponent]
public sealed partial class RotatePointLightComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Angle Angle;
}
