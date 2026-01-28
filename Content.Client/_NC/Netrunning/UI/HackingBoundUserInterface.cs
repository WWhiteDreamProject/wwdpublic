using Content.Shared._NC.Netrunning.UI;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using JetBrains.Annotations;

namespace Content.Client._NC.Netrunning.UI;

[UsedImplicitly]
public sealed class HackingBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HackingWindow? _window;

    public HackingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new HackingWindow();
        _window.OnClose += Close;

        _window.OnUseProgram += programEnt =>
        {
            SendMessage(new HackingUseProgramMessage(programEnt));
        };

        _window.OnSubmitPassword += pass =>
        {
            SendMessage(new HackingPassphraseMessage(pass));
        };

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window != null && state is HackingBoundUiState cast)
        {
            _window.UpdateState(cast);
        }
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
