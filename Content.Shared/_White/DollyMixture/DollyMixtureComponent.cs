using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.DollyMixture;


// this now lives in shared because it allows to add this component server-side and
// have it appear for all clients without additional syncing boilerplate
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DollyMixtureComponent : Component
{
    [DataField("sprite"), AutoNetworkedField]
    public string? RSIPath = null;
    public string? CurrentRSIPath = null;

    [DataField]
    public string StatePrefix = "dollymix";

    [DataField]
    public Vector2 LayerOffset = new(0, 1);

    [DataField]
    public Vector2 Offset;

    [DataField]
    public int RepeatLayers;

    public Angle LastAngle;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<string> LayerMappings = new();

    [DataField]
    public bool Enabled = true;
}
