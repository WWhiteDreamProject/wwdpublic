using Robust.Shared.GameStates;


namespace Content.Server._White.Hearing;

[RegisterComponent, NetworkedComponent]
public sealed partial class HearingComponent: Component
{
    // Used by the DeafnessSystem to apply DeafComponent to this entity
}
