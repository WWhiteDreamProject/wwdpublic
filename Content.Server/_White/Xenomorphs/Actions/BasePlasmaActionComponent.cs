namespace Content.Server._White.Xenomorphs.Actions;

[RegisterComponent]
public sealed partial class BasePlasmaActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float PlasmaCost = 50f;
}
