using Robust.Shared.GameStates;
using System.Numerics;

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

    // 0 to disable
    [DataField]
    public int DirectionCount = 0;

    [DataField]
    public float LayerHeight = 0.75f;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public int RepeatLayers = 0;

    public Angle LastAngle;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<string> LayerMappings = new();
}
