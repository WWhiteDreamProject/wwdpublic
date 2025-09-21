using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Language;
using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared.Chat;
using Content.Shared.Language;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._White.BloodCult.BloodCultist;

public sealed class BloodCultistSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, EntitySpokeEvent>(OnCultistSpeak);
    }

    private void OnCultistSpeak(EntityUid uid, BloodCultistComponent component, EntitySpokeEvent args)
    {
        if (args.Source != uid || args.Language.ID != component.CultLanguageId || args.IsWhisper)
            return;

        SendMessage(args.Source, args.Message, false, args.Language);
    }

    private void SendMessage(EntityUid source, string message, bool hideChat, LanguagePrototype language)
    {
        var clients = GetClients(language.ID);
        var playerName = Name(source);
        var wrappedMessage = Loc.GetString(
            "chat-manager-send-cult-chat-wrap-message",
            ("channelName", Loc.GetString("chat-manager-cult-channel-name")),
            ("player", playerName),
            ("message", FormattedMessage.EscapeText(message)));

        _chatManager.ChatMessageToMany(
            ChatChannel.Telepathic,
            message,
            wrappedMessage,
            source,
            hideChat,
            true,
            clients.ToList(),
            language.SpeechOverride.Color);
    }

    private IEnumerable<INetChannel> GetClients(string languageId) =>
        Filter.Empty()
            .AddWhereAttachedEntity(entity => CanHearBloodCult(entity, languageId))
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);

    private bool CanHearBloodCult(EntityUid entity, string languageId)
    {
        var understood = _language.GetUnderstoodLanguages(entity);
        return understood.Any(language => language.Id == languageId);
    }
}
