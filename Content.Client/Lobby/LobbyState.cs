using System.Linq;
using System.Numerics;
using Content.Client.Audio;
using Content.Client.Changelog;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.ReadyManifest;
using Content.Client.Resources;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.Voting;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Lobby
{
    public sealed class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly ChangelogManager _changelog = default!; // WD EDIT

        private ISawmill _sawmill = default!;
        private ClientGameTicker _gameTicker = default!;
        private ContentAudioSystem _contentAudioSystem = default!;
        private ReadyManifestSystem _readyManifest = default!;

        protected override Type? LinkedScreenType { get; } = typeof(LobbyGui);
        public LobbyGui? Lobby;

        protected override void Startup()
        {
            if (_userInterfaceManager.ActiveScreen == null)
                return;

            Lobby = (LobbyGui) _userInterfaceManager.ActiveScreen;

            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            _gameTicker = _entityManager.System<ClientGameTicker>();
            _contentAudioSystem = _entityManager.System<ContentAudioSystem>();
            _contentAudioSystem.LobbySoundtrackChanged += UpdateLobbySoundtrackInfo;
            _sawmill = Logger.GetSawmill("lobby");
            _readyManifest = _entityManager.EntitySysManager.GetEntitySystem<ReadyManifestSystem>();

            chatController.SetMainChat(true);

            _voteManager.SetPopupContainer(Lobby.VoteContainer);
            LayoutContainer.SetAnchorPreset(Lobby, LayoutContainer.LayoutPreset.Wide);
            Lobby.ServerName.Text = _baseClient.GameInfo?.ServerName; // The eye of refactor gazes upon you...

            UpdateLobbyUi();

            // Lobby.CharacterPreview.CharacterSetupButton.OnPressed += OnSetupPressed;
            Lobby.ManifestButton.OnPressed += OnManifestPressed;
            Lobby.ReadyButton.OnPressed += OnReadyPressed;
            Lobby.ReadyButton.OnToggled += OnReadyToggled;
            Lobby.CharacterSetupButton.OnPressed += OnSetupPressed;

            _gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;

            PopulateChangelog(); // WD EDIT
        }

        protected override void Shutdown()
        {

            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            chatController.SetMainChat(false);
            _gameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;
            _contentAudioSystem.LobbySoundtrackChanged -= UpdateLobbySoundtrackInfo;

            _voteManager.ClearPopupContainer();

            if (Lobby is null)
                return;

            Lobby.CharacterSetupButton.OnPressed -= OnSetupPressed;
            Lobby.ManifestButton.OnPressed -= OnManifestPressed;
            Lobby.ReadyButton.OnPressed -= OnReadyPressed;
            Lobby.ReadyButton.OnToggled -= OnReadyToggled;

            Lobby = null;
        }

        public void SwitchState(LobbyGui.LobbyGuiState state)
        {
            // Yeah I hate this but LobbyState contains all the badness for now
            Lobby?.SwitchState(state);
        }

        private void OnSetupPressed(BaseButton.ButtonEventArgs args)
        {
            SetReady(false);
            Lobby?.SwitchState(LobbyGui.LobbyGuiState.CharacterSetup);
        }

        private void OnReadyPressed(BaseButton.ButtonEventArgs args)
        {
            if (!_gameTicker.IsGameStarted)
                return;

            new LateJoinGui().OpenCentered();
        }

        private void OnReadyToggled(BaseButton.ButtonToggledEventArgs args)
        {
            SetReady(args.Pressed);
        }

        private void OnManifestPressed(BaseButton.ButtonEventArgs args)
        {
            _readyManifest.RequestReadyManifest();
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_gameTicker.IsGameStarted)
            {
                Lobby!.StartTime.Text = string.Empty;
                var roundTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                Lobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-time",
                    ("hours", roundTime.Hours),
                    ("minutes", roundTime.Minutes));
                return;
            }

            Lobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-not-started");
            string text;

            if (_gameTicker.Paused)
                text = Loc.GetString("lobby-state-paused");
            else if (_gameTicker.StartTime < _gameTiming.CurTime)
            {
                Lobby!.StartTime.Text = Loc.GetString("lobby-state-soon");
                return;
            }
            else
            {
                var difference = _gameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                    text = Loc.GetString(seconds < -5
                        ? "lobby-state-right-now-question"
                        : "lobby-state-right-now-confirmation");
                else
                    text = $"{difference.Minutes}:{difference.Seconds:D2}";
            }

            Lobby!.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void LobbyStatusUpdated()
        {
            UpdateLobbyBackground();
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            Lobby!.ReadyButton.Disabled = _gameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_gameTicker.IsGameStarted)
            {
                Lobby!.ReadyButton.ButtonText = Loc.GetString("lobby-state-ready-button-join-state");
                Lobby!.ReadyButton.ToggleMode = false;
                Lobby!.ReadyButton.Pressed = false;
                Lobby!.ObserveButton.Disabled = false;
                Lobby!.ManifestButton.Disabled = true;
            }
            else
            {
                Lobby!.ReadyButton.ButtonText = Loc.GetString(Lobby!.ReadyButton.Pressed
                    ? "lobby-state-player-status-ready"
                    : "lobby-state-player-status-not-ready");
                Lobby!.StartTime.Text = string.Empty;
                Lobby!.ReadyButton.ToggleMode = true;
                Lobby!.ReadyButton.Disabled = false;
                Lobby!.ReadyButton.Pressed = _gameTicker.AreWeReady;
                Lobby!.ManifestButton.Disabled = false;
                Lobby!.ObserveButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
                Lobby!.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);

            Lobby!.LabelName.SetMarkup("[font=\"Bedstead\" size=20] White Dream [/font]"); // WD EDIT
            Lobby!.ChangelogLabel.SetMarkup(Loc.GetString("ui-lobby-changelog")); // WD EDIT
        }

        private void UpdateLobbySoundtrackInfo(LobbySoundtrackChangedEvent ev)
        {
            if (ev.SoundtrackFilename == null)
                Lobby!.LobbySong.SetMarkup(Loc.GetString("lobby-state-song-no-song-text"));
            else if (ev.SoundtrackFilename != null
                     && _resourceCache.TryGetResource<AudioResource>(ev.SoundtrackFilename, out var lobbySongResource))
            {
                var lobbyStream = lobbySongResource.AudioStream;

                var title = string.IsNullOrEmpty(lobbyStream.Title)
                    ? Loc.GetString("lobby-state-song-unknown-title")
                    : lobbyStream.Title;

                var artist = string.IsNullOrEmpty(lobbyStream.Artist)
                    ? Loc.GetString("lobby-state-song-unknown-artist")
                    : lobbyStream.Artist;

                var markup = Loc.GetString("lobby-state-song-text",
                    ("songTitle", title),
                    ("songArtist", artist));

                Lobby!.LobbySong.SetMarkup(markup);
            }
        }

        private void UpdateLobbyBackground()
        {
            if (_gameTicker.AnimatedLobbyScreen != null) // WD EDIT
            {
                Lobby!.Background.SetRSI(_resourceCache.GetResource<RSIResource>(_gameTicker.AnimatedLobbyScreen.Path).RSI); // WD EDIT

                var lobbyBackground = _gameTicker.AnimatedLobbyScreen; // WD EDIT

                var name = string.IsNullOrEmpty(lobbyBackground.Name)
                    ? Loc.GetString("lobby-state-background-unknown-title")
                    : lobbyBackground.Name;

                var artist = string.IsNullOrEmpty(lobbyBackground.Artist)
                    ? Loc.GetString("lobby-state-background-unknown-artist")
                    : lobbyBackground.Artist;

                var markup = Loc.GetString("lobby-state-background-text",
                    ("backgroundName", name),
                    ("backgroundArtist", artist));

                Lobby!.LobbyBackground.SetMarkup(markup);

                return;
            }

            _sawmill.Warning("_gameTicker.LobbyBackground was null! No lobby background selected.");
            Lobby!.Background.Texture = null;
            Lobby!.LobbyBackground.SetMarkup(Loc.GetString("lobby-state-background-no-background-text"));
        }

        private void SetReady(bool newReady)
        {
            if (_gameTicker.IsGameStarted)
                return;

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
        }

        private async void PopulateChangelog()
        {
            if (Lobby?.ChangelogContainer?.Children is null)
                return;

            Lobby.ChangelogContainer.Children.Clear();

            var changelogs = await _changelog.LoadChangelog();
            var whiteChangelog = changelogs.Find(cl => cl.Name == "WhiteChangelog");

            if (whiteChangelog is null)
            {
                Lobby.ChangelogContainer.Children.Add(
                    new RichTextLabel().SetMarkup(Loc.GetString("ui-lobby-changelog-not-found")));

                return;
            }

            var entries = whiteChangelog.Entries
                .OrderByDescending(c => c.Time)
                .Take(3);

            foreach (var entry in entries)
            {
                var box = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = Control.HAlignment.Left,
                    Children =
                    {
                        new Label
                        {
                            Align = Label.AlignMode.Left,
                            Text = $"{entry.Author} {entry.Time.ToShortDateString()}",
                            FontColorOverride = Color.FromHex("#888"),
                            Margin = new Thickness(0, 10)
                        }
                    }
                };

                foreach (var change in entry.Changes)
                {
                    var container = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        HorizontalAlignment = Control.HAlignment.Left
                    };

                    var text = new RichTextLabel();
                    text.SetMessage(FormattedMessage.FromMarkup(change.Message));
                    text.MaxWidth = 350;

                    container.AddChild(GetIcon(change.Type));
                    container.AddChild(text);

                    box.AddChild(container);
                }

                if (Lobby?.ChangelogContainer is null)
                    return;

                Lobby.ChangelogContainer.AddChild(box);
            }
        }

        private TextureRect GetIcon(ChangelogManager.ChangelogLineType type)
        {
            var (file, color) = type switch
            {
                ChangelogManager.ChangelogLineType.Add => ("plus.svg.192dpi.png", "#6ED18D"),
                ChangelogManager.ChangelogLineType.Remove => ("minus.svg.192dpi.png", "#D16E6E"),
                ChangelogManager.ChangelogLineType.Fix => ("bug.svg.192dpi.png", "#D1BA6E"),
                ChangelogManager.ChangelogLineType.Tweak => ("wrench.svg.192dpi.png", "#6E96D1"),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return new TextureRect
            {
                Texture = _resourceCache.GetTexture(new ResPath($"/Textures/Interface/Changelog/{file}")),
                VerticalAlignment = Control.VAlignment.Top,
                TextureScale = new Vector2(0.5f, 0.5f),
                Margin = new Thickness(2, 4, 6, 2),
                ModulateSelfOverride = Color.FromHex(color)
            };
        }
        // WD EDIT END
    }
}
