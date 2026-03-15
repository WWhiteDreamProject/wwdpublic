using Content.Shared._NC.Citation;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Citation;

public sealed class CitationDeviceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CitationDeviceWindow? _window;

    public CitationDeviceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new CitationDeviceWindow();
        _window.OnClose += Close;
        _window.OnSubmit += (amount, reason) => SendMessage(new CitationDeviceCreateMessage(amount, reason));

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CitationDeviceBuiState msg) return;

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