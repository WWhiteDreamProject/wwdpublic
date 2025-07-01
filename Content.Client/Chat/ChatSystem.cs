using Content.Shared.Chat;
using System.Text.RegularExpressions;

namespace Content.Client.Chat;

public void OnPlayerChat(EntityUid player, string message)
{
    var bbcodePattern = new Regex(@"\[(\/?)(b|i|u|color|size|spoiler)(=[^\]]+)?\]", RegexOptions.IgnoreCase);

    if (bbcodePattern.IsMatch(message))
    {
        ShowPlayerMessage(player, "BBCode-теги запрещены в чате.");
        return;
    }

    SendToChat(player, message);
}

public sealed class ChatSystem : SharedChatSystem {}
