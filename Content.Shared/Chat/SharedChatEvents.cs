using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Radio;

namespace Content.Shared.Chat;

/// <summary>
///     This event should be sent everytime an entity talks (Radio, local chat, etc...).
///     The event is sent to both the entity itself, and all clothing (For stuff like voice masks).
/// </summary>
public sealed class TransformSpeakerNameEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender;
    public string VoiceName;
    public ProtoId<SpeechVerbPrototype>? SpeechVerb;
    public readonly RadioChannelPrototype? Channel; // WD EDIT

    public TransformSpeakerNameEvent(EntityUid sender, string name, RadioChannelPrototype? channel = null) // WD EDIT
    {
        Sender = sender;
        VoiceName = name;
        SpeechVerb = null;
        Channel = channel;
    }
}

