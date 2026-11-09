using Content.Shared._White.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabThrownComponent : Component
{
    public DamageSpecifier? DamageOnCollide;

    public List<EntityUid> IgnoreEntity = new();
}
