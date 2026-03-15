using Content.Shared._NC.Forensics;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._NC.Forensics;

public sealed class NcpdForensicsConsoleBoundUserInterface : BoundUserInterface
{
    private NcpdForensicsConsoleWindow? _window;

    public NcpdForensicsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new NcpdForensicsConsoleWindow();
        _window.OnClose += Close;
        _window.OnAlertAction += (index, action) => SendMessage(new NcpdForensicsAlertActionMessage(index, action));
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not NcpdForensicsConsoleBuiState s)
            return;
        _window.UpdateState(s);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _window?.Close();
        _window = null;
    }
}
