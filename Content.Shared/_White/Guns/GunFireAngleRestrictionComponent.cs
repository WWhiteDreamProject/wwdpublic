using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class GunFireAngleRestrictionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}

