using System.Linq;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;

namespace Content.Shared.Announcements.Components;

[RegisterComponent]
[Serializable]
public sealed partial class TimedAnnouncementComponent : Component
{
    [DataField("sender")]
    public string Sender { get; set; } = "chat-manager-sender-announcement";

    [DataField("sound")]
    public SoundSpecifier? Sound { get; set; }

    [DataField("messageColor")]
    public Color MessageColor { get; set; } = Color.White;

    [DataField("announcements")]
    public List<TimedAnnouncementData> Announcements { get; set; } = new();

    [DataField("cycle")]
    public float Cycle { get; set; } = 0f;

    [DataField("repeat")]
    public int Repeat { get; set; } = 1;
}

[Serializable]
[DataDefinition]
public sealed partial class TimedAnnouncementData
{
    [DataField("delay")]
    public float Delay { get; set; }

    [DataField("announcement")]
    public string Announcement { get; set; } = string.Empty;

    [DataField("sender")]
    public string? Sender { get; set; }

    [DataField("sound")]
    public SoundSpecifier? Sound { get; set; }

    [DataField("messageColor")]
    public Color? MessageColor { get; set; }
}
