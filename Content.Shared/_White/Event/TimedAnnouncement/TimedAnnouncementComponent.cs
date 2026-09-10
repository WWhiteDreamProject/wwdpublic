using System.Linq;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;

namespace Content.Shared.Announcements.Components;

[RegisterComponent]
[Serializable]
public sealed partial class TimedAnnouncementComponent : Component
{
    [DataField]
    public string Sender { get; set; } = "chat-manager-sender-announcement";

    [DataField]
    public SoundSpecifier? Sound { get; set; }

    [DataField]
    public Color MessageColor { get; set; } = Color.White;

    [DataField]
    public List<TimedAnnouncementData> Announcements { get; set; } = new();

    [DataField]
    public float Cycle { get; set; } = 0f;

    [DataField]
    public int Repeat { get; set; } = 1;
}

[Serializable]
[DataDefinition]
public sealed partial class TimedAnnouncementData
{
    [DataField]
    public float Delay { get; set; }

    [DataField]
    public string Announcement { get; set; } = string.Empty;

    [DataField]
    public string? Sender { get; set; }

    [DataField]
    public SoundSpecifier? Sound { get; set; }

    [DataField]
    public Color? MessageColor { get; set; }
}
