using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._White.UI.Buttons;

public class WhiteUICommandButton : WhiteCommandButton
{
    public Type? WindowType { get; set; }
    private DefaultWindow? _window;

    protected override void Execute(ButtonEventArgs obj)
    {
        if (WindowType == null)
            return;

        _window = (DefaultWindow) IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance(WindowType);
        _window?.OpenCentered();
    }
}
