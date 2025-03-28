namespace Content.Server._White.Hearing;

/// <summary>
/// Changes all incoming chat messages to DeafChatMessage.
/// Added by the DeafnessSystem
/// </summary>
[RegisterComponent]
public sealed partial class DeafComponent : Component
{
    [DataField]
    public LocId DeafChatMessage = "deaf-chat-message";

    [DataField]
    public bool AlwaysDeaf = false; // Allows to make someone permanently deaf with VV, for admemes
}
