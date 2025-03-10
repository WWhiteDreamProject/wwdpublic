using System.Numerics;
using Content.Client.Chat.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared._White;
using Content.Shared._White.UI.Emotes;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client._White.UI.Emotes;

public sealed class WhiteEmotesUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private IBaseEmoteMenu? _window;
    private MenuButton? EmotesButton => UIManager.GetActiveUIWidgetOrNull<UserInterface.Systems.MenuBar.Widgets.GameTopMenuBar>()?.EmotesButton;

    private DateTime _lastEmotionTimeUse = DateTime.Now;
    private const float EmoteCooldown = 1.5f;

    public void OnStateEntered(GameplayState state) =>
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenEmotesMenu, InputCmdHandler.FromDelegate(_ => ToggleWindow(false)))
            .Register<WhiteEmotesUIController>();

    public void OnStateExited(GameplayState state) =>
        CommandBinds.Unregister<WhiteEmotesUIController>();

    public void LoadButton()
    {
        if (EmotesButton == null)
            return;

        EmotesButton.OnPressed += EmotionsButtonPressed;
    }

    public void UnloadButton()
    {
        if (EmotesButton == null)
            return;

        EmotesButton.OnPressed -= EmotionsButtonPressed;
    }

    private void OnWindowOpened()
    {
        if (EmotesButton != null)
            EmotesButton.Pressed = true;
    }

    private void OnWindowClosed()
    {
        if (EmotesButton != null)
            EmotesButton.Pressed = false;

        CloseWindow();
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        _window.Dispose();
        _window = null;
    }

    private void EmotionsButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow(true);
    }

    private void ToggleWindow(bool centered)
    {
        if (_window == null)
        {
            if (!Enum.TryParse(_configurationManager.GetCVar(WhiteCVars.EmotesMenuStyle), out EmotesMenuType emotesMenuStyle))
                emotesMenuStyle = EmotesMenuType.Window;

            // setup window
            switch (emotesMenuStyle)
            {
                case EmotesMenuType.Window:
                    _window = UIManager.CreateWindow<WhiteEmotesMenu>();
                    break;
                case EmotesMenuType.Radial:
                    _window = UIManager.CreateWindow<EmotesMenu>();
                    break;
                default:
                    _window = UIManager.CreateWindow<WhiteEmotesMenu>();
                    break;
            }

            _window.OnClose += OnWindowClosed;
            _window.OnOpen += OnWindowOpened;
            _window.OnPlayEmote += OnPlayEmote;

            if (EmotesButton != null)
                EmotesButton.SetClickPressed(true);

            if (centered)
            {
                _window.OpenCentered();
            }
            else
            {
                // Open the menu, centered on the mouse
                var vpSize = _displayManager.ScreenSize;
                _window.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
            }
        }
        else
        {
            _window.OnClose -= OnWindowClosed;
            _window.OnOpen -= OnWindowOpened;
            _window.OnPlayEmote -= OnPlayEmote;

            if (EmotesButton != null)
                EmotesButton.SetClickPressed(false);

            CloseWindow();
        }
    }

    private void OnPlayEmote(ProtoId<EmotePrototype> protoId)
    {
        var timeSpan = DateTime.Now - _lastEmotionTimeUse;
        var seconds = timeSpan.TotalSeconds;
        if (seconds < EmoteCooldown)
            return;

        _lastEmotionTimeUse = DateTime.Now;

        _entityManager.RaisePredictiveEvent(new PlayEmoteMessage(protoId));
    }
}

public interface IBaseEmoteMenu
{
    public event Action<ProtoId<EmotePrototype>>? OnPlayEmote;
    public event Action? OnClose;
    public event Action? OnOpen;
    public void OpenCenteredAt(Vector2 relativePosition);
    public void OpenCentered();
    public void Dispose();
}
