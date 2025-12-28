using Content.Shared._White.Body.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundableComponent : Component
{
    /// <summary>
    /// List of all body wounds.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<BodyPartType, IReadOnlyList<EntityUid>> Wounds = new();
}
