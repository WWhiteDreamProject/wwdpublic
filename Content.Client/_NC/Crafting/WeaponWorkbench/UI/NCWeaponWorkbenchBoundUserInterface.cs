using Content.Shared._NC.Crafting.WeaponWorkbench.Events;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._NC.Crafting.WeaponWorkbench.UI;

public sealed class NCWeaponWorkbenchBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private NCWeaponWorkbenchWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new NCWeaponWorkbenchWindow();

        if (State != null)
            UpdateState(State);

        _window.OpenCentered();
        _window.OnClose += Close;

        _window.OnOperatorCommand += OnCommandPressed;
        _window.OnLockCodeSubmit += OnLockCodeSubmitted;
    }

    private void OnCommandPressed(OperatorCommandType obj) => SendMessage(new NCWorkbenchOperatorCommandMessage(obj));
    private void OnLockCodeSubmitted(string code) => SendMessage(new NCWorkbenchLockCodeInputMessage(code));

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || !_window.IsOpen)
            return;

        if (state is NCWeaponWorkbenchUpdateState workbenchState)
        {
            _window.UpdateState(workbenchState);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Close();
        }
    }
}
