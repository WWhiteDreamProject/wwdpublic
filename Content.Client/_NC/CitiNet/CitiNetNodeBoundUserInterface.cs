using Content.Shared._NC.CitiNet;
using Robust.Client.GameObjects;

namespace Content.Client._NC.CitiNet;

public sealed class CitiNetNodeBoundUserInterface : BoundUserInterface
{
    private CitiNetNodeWindow? _window;

    public CitiNetNodeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new CitiNetNodeWindow();
        _window.OnClose += Close;
        _window.OnEmergencyExtraction += () => SendMessage(new CitiNetNodeEmergencyExtractionMessage());
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CitiNetNodeBoundUserInterfaceState nodeState)
            return;

        _window?.UpdateState(nodeState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
