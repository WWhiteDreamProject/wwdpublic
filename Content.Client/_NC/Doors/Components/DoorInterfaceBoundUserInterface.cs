using Content.Shared._NC.Doors;
using Content.Shared._NC.Doors.Components;
using Content.Client._NC.Doors.UI;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.IoC;
using Robust.Shared.GameStates;
using JetBrains.Annotations;

namespace Content.Client._NC.Doors.Components;

[UsedImplicitly]
public sealed class DoorInterfaceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DoorInterfaceWindow? _window;

    public DoorInterfaceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new DoorInterfaceWindow();
        _window.OnClose += Close;
        _window.OnAction += OnAction;
        _window.OnLockAction += OnLockAction;
        _window.OpenCentered();
    }

    private void OnAction()
    {
        if (_window == null) return;
        if (_window.IsBuyAction)
        {
            SendMessage(new DoorInterfaceBuyMessage());
        }
        else
        {
            SendMessage(new DoorInterfaceSellMessage());
        }
    }

    private void OnLockAction()
    {
        SendMessage(new DoorInterfaceLockMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DoorInterfaceState castState)
            return;

        var playerMan = IoCManager.Resolve<IPlayerManager>();
        var user = playerMan.LocalSession?.UserId;
        bool isOwner = user != null && castState.OwnerId == user;

        _window?.UpdateState(castState, isOwner);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}
