namespace Content.Server._White.Body;

[RegisterComponent]
public sealed partial class InfectedBodyComponent : Component
{
    [ViewVariables]
    public EntityUid InfectiousOrgan;
}
