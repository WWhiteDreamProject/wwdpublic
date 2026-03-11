using Content.Shared._NC.Forensics;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Forensics;

public sealed class BallisticAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BallisticAnalyzerWindow? _window;

    public BallisticAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new BallisticAnalyzerWindow();
        _window.OnClose += Close;
        _window.OnStartAnalyze += () => SendMessage(new BallisticAnalyzerStartMessage());

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BallisticAnalyzerBuiState msg) return;

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
