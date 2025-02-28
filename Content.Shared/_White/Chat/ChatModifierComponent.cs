namespace Content.Shared.Chat;

/// <summary>
/// WWDP
/// </summary>

[RegisterComponent]
public sealed partial class ChatModifierComponent : Component
{
    [DataField("whisperListeningRange")]
    public int WhisperListeningRange = SharedChatSystem.WhisperClearRange;
}
