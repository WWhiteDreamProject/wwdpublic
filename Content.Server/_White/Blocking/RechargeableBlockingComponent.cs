namespace Content.Server._White.Blocking;

[RegisterComponent]
public sealed partial class RechargeableBlockingComponent : Component
{
    [DataField]
    public float RechargeDelay = 30f;

    [ViewVariables]
    public bool Discharged;
}
