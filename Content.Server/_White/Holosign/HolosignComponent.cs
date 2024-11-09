namespace Content.Server._White.Holosign;

[RegisterComponent]
public sealed partial class HolosignComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Projector;
}
