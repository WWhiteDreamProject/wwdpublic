using Content.Client._White.CustomGhosts.UI;
using Content.Client._White.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._White.UserInterface.Buttons;

public sealed class CustomGhostsMenuOpenButton : Button
{
    WindowTracker<CustomGhostsWindow> _customGhostWindow = new();
    public CustomGhostsMenuOpenButton() : base()
    {
        OnPressed += Pressed;
    }

    private void Pressed(ButtonEventArgs args)
    {
        _customGhostWindow.TryOpenCenteredLeft();
    }
}

