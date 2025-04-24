using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Stealth;

[RegisterComponent, NetworkedComponent]
public sealed partial class StealthOnWalkComponent : Component
{
    [DataField]
    public FixedPoint2 PlasmaCost;

    [ViewVariables]
    public bool Stealth;
}
