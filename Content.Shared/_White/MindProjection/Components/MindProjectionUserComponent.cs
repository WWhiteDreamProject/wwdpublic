using Robust.Shared.GameStates;

namespace Content.Shared._White.MindProjection.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MingProjectingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Target;

    [ViewVariables, AutoNetworkedField]
    public EntityUid Console;
}
