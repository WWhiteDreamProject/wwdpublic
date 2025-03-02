using System.Threading;
using Robust.Shared.GameStates;


namespace Content.Server._White.Hearing;

/// <summary>
/// Changes all incoming chat messages to DeafChatMessage.
/// Added by the DeafnessSystem on the HearingChangedEvent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeafComponent : Component
{
    [DataField]
    public LocId DeafChatMessage = "deaf-chat-message";

    [DataField]
    public bool Permanent = false;

    [DataField]
    public float Duration = 0f; // In seconds

    public CancellationTokenSource? TokenSource;
}
