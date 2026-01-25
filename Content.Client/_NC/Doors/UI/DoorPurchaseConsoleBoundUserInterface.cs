using Content.Shared._NC.Doors.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Client._NC.Doors.UI;

public sealed class DoorPurchaseConsoleBoundUserInterface : BoundUserInterface
{
    private DoorPurchaseConsoleWindow? _window;

    public DoorPurchaseConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<DoorPurchaseConsoleWindow>();

        // Hook into the NavMap's selection event directly
        if (_window.Map != null)
        {
            _window.Map.TrackedEntitySelectedAction += OnDoorSelected;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DoorPurchaseConsoleState cast || _window == null)
            return;

        _window.UpdateState(cast, EntMan);
    }

    private void OnDoorSelected(NetEntity? result)
    {
        if (result != null)
        {
            SendMessage(new DoorPurchaseConsoleOpenInterfaceMessage(result.Value));
        }
    }
}
