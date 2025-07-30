using Content.Client._White.RadialSelector;
using Content.Client.UserInterface.Controls;
using Content.Shared.RadialSelector;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;

// ReSharper disable InconsistentNaming

namespace Content.Client.RadialSelector;

[UsedImplicitly]
public sealed class AttachedRadialSelectorMenuBUI(EntityUid owner, Enum uiKey)
    : BasedRadialSelectorMenuBUI(owner, uiKey) // WD EDIT: make it more generic
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private readonly RadialMenu _menu = new()
    {
        HorizontalExpand = true,
        VerticalExpand = true,
        BackButtonStyleClass = "RadialMenuBackButton",
        CloseButtonStyleClass = "RadialMenuCloseButton"
    };

    private bool _openCentered;

    protected override void Open()
    {
        base.Open(); //WWDP EDIT - Summary: Overriders MUST ALWAYS call base function
        _menu.OnClose += Close;

        if (_openCentered)
            _menu.OpenCentered();
        else
            _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / _displayManager.ScreenSize);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RadialSelectorState radialSelectorState)
            return;

        ClearExistingContainers(_menu);
        CreateMenu(radialSelectorState.Entries, _menu);
        _openCentered = radialSelectorState.OpenCentered;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _menu.Dispose();
    }
}
