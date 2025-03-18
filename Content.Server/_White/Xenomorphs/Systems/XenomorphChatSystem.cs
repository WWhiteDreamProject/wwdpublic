using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Language;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Chat;
using Content.Shared.Language;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class XenomorphsChatSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlienComponent, EntitySpokeEvent>(OnAlienSpeak);
    }

    private void OnAlienSpeak(EntityUid uid, AlienComponent component, EntitySpokeEvent args)
    {
        if (args.Source != uid || args.Language.ID != component.XenoLanguageId || args.IsWhisper)
            return;

        SendMessage(args.Source, args.Message, false, args.Language);
    }

    private void SendMessage(EntityUid source, string message, bool hideChat, LanguagePrototype language)
    {
        var clients = GetClients(language.ID);
        var playerName = Name(source);
        var wrappedMessage = Loc.GetString("chat-manager-send-xeno-hivemind-chat-wrap-message",
            ("channelName", Loc.GetString("chat-manager-xeno-hivemind-channel-name")),
            ("player", playerName),
            ("message", FormattedMessage.EscapeText(message)));

        _chatManager.ChatMessageToMany(ChatChannel.Telepathic,
            message,
            wrappedMessage,
            source,
            hideChat,
            true,
            clients.ToList(),
            language.SpeechOverride.Color);
    }

    private IEnumerable<INetChannel> GetClients(string languageId)
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(entity => CanHearXenoHivemind(entity, languageId))
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }

    private bool CanHearXenoHivemind(EntityUid entity, string languageId)
    {
        var understood = _language.GetUnderstoodLanguages(entity);
        return understood.Any(language => language.Id == languageId);
    }
}

