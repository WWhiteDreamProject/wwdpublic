using Content.Shared._NC.Netrunning.UI;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using System;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client._NC.Netrunning.UI;

public sealed class NetMapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NetMapWindow? _window;

    public NetMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new NetMapWindow();
        _window.OnClose += Close;
        _window.OnInteract += (target, action) => SendMessage(new NetMapInteractMessage(target, action));
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is NetMapBoundUiState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}
