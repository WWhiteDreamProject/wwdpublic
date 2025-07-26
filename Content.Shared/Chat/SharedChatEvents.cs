using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory;

namespace Content.Shared.Chat;

/// <summary>
///     This event should be sent everytime an entity talks (Radio, local chat, etc...).
///     The event is sent to both the entity itself, and all clothing (For stuff like voice masks).
/// </summary>
public class TransformSpeakerNameEvent : EntityEventArgs, IInventoryRelayEvent // WWDP add sealed
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender;
    public string VoiceName;
    public ProtoId<SpeechVerbPrototype>? SpeechVerb;

    public TransformSpeakerNameEvent(EntityUid sender, string name)
    {
        Sender = sender;
        VoiceName = name;
        SpeechVerb = null;
    }
}

/// <summary>
/// WWDP add
/// </summary>
[ByRefEvent]
public sealed class TransformRadioSpeakerNameEvent : TransformSpeakerNameEvent
{
    public readonly Shared.Radio.RadioChannelPrototype Channel;

    public TransformRadioSpeakerNameEvent(EntityUid sender, string name, Shared.Radio.RadioChannelPrototype channel) : base(sender, name)
    {
        Channel = channel;
    }
}
