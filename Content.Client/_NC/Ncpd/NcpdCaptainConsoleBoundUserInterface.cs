using Content.Shared._NC.Ncpd;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Ncpd;

public sealed class NcpdCaptainConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NcpdCaptainConsoleWindow? _window;

    public NcpdCaptainConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new NcpdCaptainConsoleWindow();
        _window.OnClose += Close;
        
        _window.OnPurchase += id => SendMessage(new NcpdPurchaseMessage(id));
        _window.OnRevoke += target => SendMessage(new NcpdRevokeAccessMessage(target));
        _window.OnClearLogs += () => SendMessage(new NcpdClearLogsMessage());

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NcpdCaptainConsoleBuiState msg) return;

        _window?.UpdateState(msg);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
}