using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._White;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._White.Ghost;

public sealed class GhostReturnToRoundSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public readonly Dictionary<NetUserId, TimeSpan> DeathTime = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<GhostReturnToRoundRequest>(OnGhostReturnToRoundRequest);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(ResetDeathTimes);
    }

    private void OnGhostReturnToRoundRequest(GhostReturnToRoundRequest msg, EntitySessionEventArgs args)
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();
        var maxPlayers = cfg.GetCVar(WhiteCVars.GhostRespawnMaxPlayers);
        var connectedClient = args.SenderSession.ConnectedClient;
        if (_playerManager.PlayerCount >= maxPlayers)
        {
            var message = Loc.GetString("ghost-respawn-max-players", ("players", maxPlayers));
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                connectedClient,
                Color.Red);
            return;
        }

        var userId = args.SenderSession.UserId;
        if (!DeathTime.TryGetValue(userId, out var deathTime))
        {
            var message = Loc.GetString("ghost-respawn-bug");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                connectedClient,
                Color.Red);
            DeathTime[userId] = _gameTiming.CurTime;
            return;
        }

        var timeUntilRespawn = (double) cfg.GetCVar(WhiteCVars.GhostRespawnTime);
        var timePast = (_gameTiming.CurTime - deathTime).TotalMinutes;
        if (timePast >= timeUntilRespawn)
        {
            var ticker = Get<GameTicker>();
            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            playerMgr.TryGetSessionById(userId, out var targetPlayer);

            if (targetPlayer != null)
                ticker.Respawn(targetPlayer);
            DeathTime.Remove(userId);

            _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{Loc.GetString("ghost-respawn-log-return-to-lobby", ("userName", connectedClient.UserName))}");

            var message = Loc.GetString("ghost-respawn-window-rules-footer");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                connectedClient,
                Color.Red);

        }
        else
        {
            var message = Loc.GetString("ghost-respawn-time-left", ("time", (int) (timeUntilRespawn - timePast)));
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                connectedClient,
                Color.Red);
        }
    }

    private void ResetDeathTimes(RoundRestartCleanupEvent ev)
    {
        DeathTime.Clear();
    }
}
