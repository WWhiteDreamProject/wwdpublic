/*
 * DISABLED IN FAVOR OF EMOTES RADIAL MENU
 */


// using System.Linq;
// using Content.Client.Chat.Managers;
// using Content.Client.Gameplay;
// using Content.Client.UserInterface.Controls;
// using Content.Shared.Chat;
// using Content.Shared.Chat.Prototypes;
// using Content.Shared.Input;
// using Robust.Client.UserInterface.Controllers;
// using Robust.Client.UserInterface.Controls;
// using Robust.Client.UserInterface.CustomControls;
// using Robust.Shared.Input.Binding;
// using Robust.Shared.Prototypes;
// using Robust.Shared.Random;
//
// namespace Content.Client._White.UI.Emotions;
//
// public sealed class EmotionsUIController : UIController, IOnStateChanged<GameplayState>
// {
//     [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
//     [Dependency] private readonly IRobustRandom _random = default!;
//     [Dependency] private readonly IChatManager _chatManager = default!;
//
//     private DefaultWindow? _window;
//     private MenuButton? EmotionsButton => UIManager.GetActiveUIWidgetOrNull<UserInterface.Systems.MenuBar.Widgets.GameTopMenuBar>()?.EmotionsButton;
//
//     private DateTime _lastEmotionTimeUse = DateTime.Now;
//     private const float EmoteCooldown = 1.5f;
//
//     public void OnStateEntered(GameplayState state)
//     {
//         _window = FormMenu();
//
//         _window.OnOpen += OnWindowOpened;
//         _window.OnClose += OnWindowClosed;
//
//         CommandBinds.Builder
//             .Bind(ContentKeyFunctions.OpenEmotesMenu, InputCmdHandler.FromDelegate(_ => ToggleWindow()))
//             .Register<EmotionsUIController>();
//    }
//
//     public void OnStateExited(GameplayState state)
//     {
//         if (_window != null)
//         {
//             _window.OnOpen -= OnWindowOpened;
//             _window.OnClose -= OnWindowClosed;
//
//             _window.Dispose();
//             _window = null;
//         }
//
//         CommandBinds.Unregister<EmotionsUIController>();
//     }
//
//     public void LoadButton()
//     {
//         if (EmotionsButton == null)
//             return;
//
//         EmotionsButton.OnPressed += EmotionsButtonPressed;
//     }
//
//     public void UnloadButton()
//     {
//         if (EmotionsButton == null)
//             return;
//
//         EmotionsButton.OnPressed -= EmotionsButtonPressed;
//     }
//
//     private void OnWindowOpened()
//     {
//         if (EmotionsButton != null)
//             EmotionsButton.Pressed = true;
//     }
//
//     private void OnWindowClosed()
//     {
//         if (EmotionsButton != null)
//             EmotionsButton.Pressed = false;
//     }
//
//     private void EmotionsButtonPressed(BaseButton.ButtonEventArgs args)
//     {
//         ToggleWindow();
//     }
//
//     private void ToggleWindow()
//     {
//         if (_window == null)
//             return;
//
//         if (_window.IsOpen)
//         {
//             _window.Close();
//             return;
//         }
//
//         _window.Open();
//     }
//
//     private void UseEmote(string emote)
//     {
//         var time = (DateTime.Now - _lastEmotionTimeUse).TotalSeconds;
//         if (time < EmoteCooldown)
//             return;
//
//         _lastEmotionTimeUse = DateTime.Now;
//         _chatManager.SendMessage(emote, ChatSelectChannel.Emotes);
//     }
//
//     private Button CreateEmoteButton(EmotePrototype emote)
//     {
//         var control = new Button
//         {
//             ClipText = true,
//             HorizontalExpand = true,
//             VerticalExpand = true,
//             MinWidth = 120,
//             MaxWidth = 250,
//             MaxHeight = 35,
//             TextAlign = Label.AlignMode.Left,
//             Text = Loc.GetString(emote.ButtonText)
//         };
//
//         control.OnPressed += _ => UseEmote(Loc.GetString(_random.Pick(emote.ChatMessages)));
//         return control;
//     }
//
//     private DefaultWindow FormMenu()
//     {
//         var window = new DefaultWindow
//         {
//             Title = Loc.GetString("emotions-menu-title"),
//             VerticalExpand = true,
//             HorizontalExpand = true,
//             MinHeight = 250,
//             MinWidth = 300
//         };
//
//         var grid = new GridContainer
//         {
//             Columns = 3
//         };
//
//         var emotions = _prototypeManager.EnumeratePrototypes<EmotePrototype>().ToList();
//         emotions.Sort((a,b) => string.Compare(Loc.GetString(a.ButtonText), Loc.GetString(b.ButtonText.ToString()), StringComparison.Ordinal));
//
//         foreach (var emote in emotions)
//         {
//             if (!emote.AllowToEmotionsMenu)
//                 continue;
//
//             var button = CreateEmoteButton(emote);
//             grid.AddChild(button);
//         }
//
//         window.Contents.AddChild(grid);
//         return window;
//     }
// }
