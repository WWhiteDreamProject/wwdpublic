using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WoundableSystem))]
public sealed partial class WoundableComponent : Component
{
    /// <summary>
    /// List of all wounds.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Wounds = new();
}
