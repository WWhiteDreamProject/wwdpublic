using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Shared.Audio;
using Robust.Shared.Localization;


namespace Content.Server.DeviceLinking.Components
{
    [RegisterComponent]
    public sealed partial class TimerAnnouncementComponent : Component
    {
        [DataField]
        public string? StartMessage;

        [DataField]
        public string? EndMessage;

        [DataField]
        public string? CancelMessage;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sender")]
        public string Sender = "Timer";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("senderColor")]
        public string SenderColor = "#FFFFFF";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("senderFont")]
        public string? SenderFont = null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("senderFontSize")]
        public int SenderFontSize = 14;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("textColor")]
        public string TextColor = "#FFD700";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("messageFont")]
        public string? MessageFont = null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("fontSize")]
        public int FontSize = 14;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier? Sound = null;
    }
}
