using Robust.Shared.GameStates;

namespace Content.Shared._White.Xenomorphs.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BasePlasmaCostActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float PlasmaCost = 50f;
}
