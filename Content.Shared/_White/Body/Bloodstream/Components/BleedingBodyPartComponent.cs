using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Body.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BleedingBodyPartComponent : Component
{
    /// <summary>
    /// The current amount of bleeding.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Bleeding;
}
