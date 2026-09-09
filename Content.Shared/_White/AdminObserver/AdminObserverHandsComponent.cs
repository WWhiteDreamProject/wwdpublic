using Robust.Shared.GameStates;

namespace Content.Shared._White.AdminObserver;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdminObserverHandsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool HandsEnabled = true;
}
