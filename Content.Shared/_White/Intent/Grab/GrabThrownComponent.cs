using Robust.Shared.GameStates;
using Content.Shared.Damage;

namespace Content.Shared._White.Intent.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabThrownComponent : Component
{
    public DamageSpecifier? DamageOnCollide;

    public DamageSpecifier? WallDamageOnCollide;

    public float? StaminaDamageOnCollide;
}
