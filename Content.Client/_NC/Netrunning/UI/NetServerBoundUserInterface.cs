using Content.Shared._NC.Netrunning.UI;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Netrunning.UI;

public sealed class NetServerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NetServerWindow? _window;

    public NetServerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new NetServerWindow();
        _window.OnClose += Close;
        _window.OnPasswordSet += (slot, pass) => SendMessage(new NetServerSetPasswordMessage(slot, pass));
        _window.OnOpenMap += () => SendMessage(new NetServerOpenMapMessage());

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NetServerBoundUiState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
