using Content.Client._White.CustomGhosts.UI;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._White.UI.Buttons;

public sealed class CustomGhostsMenuOpenButton : Button
{
    CustomGhostsWindow? _window = null;
    public CustomGhostsMenuOpenButton() : base()
    {
        OnPressed += Pressed;
    }

    private void Pressed(ButtonEventArgs args)
    {
        if (_window is not null)
        {
            _window.Close();
            _window = null;
            return;
        }

        _window = new();
        _window.OnClose += () => _window = null;
        _window.OpenCenteredLeft();
    }
}

