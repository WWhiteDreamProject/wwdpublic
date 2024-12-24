using Content.Shared.Standing;

namespace Content.Shared._White.Collision.LayDown;

[RegisterComponent]
public sealed partial class LayDownOnCollideComponent : Component
{
    [DataField]
    public DropHeldItemsBehavior Behavior = DropHeldItemsBehavior.NoDrop;
}
