using Content.Shared._NC.CitiNet;
using Content.Client.UserInterface.Controls;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using System.Numerics;

namespace Content.Client._NC.CitiNet.UI;

/// <summary>
/// Tactical Console BUI for CitiNet Map.
/// Pure C# implementation.
/// </summary>
public sealed class CitiNetMapConsoleBoundUserInterface : BoundUserInterface
{
    private CitiNetMapConsoleWindow? _window;

    public CitiNetMapConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = new CitiNetMapConsoleWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CitiNetMapBoundUserInterfaceState mapState)
            return;

        _window?.UpdateState(mapState);
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

/// <summary>
/// Fancy Window for the Tactical Map, built with pure C#.
/// </summary>
public sealed class CitiNetMapConsoleWindow : FancyWindow
{
    private readonly CitiNetMapControl _mapControl;

    public CitiNetMapConsoleWindow()
    {
        Title = "CITINET TACTICAL OVERWATCH";
        SetSize = new Vector2(1024, 768);

        // Core Layout
        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _mapControl = new CitiNetMapControl
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        root.AddChild(_mapControl);
        ContentsContainer.AddChild(root);
    }

    public void UpdateState(CitiNetMapBoundUserInterfaceState state)
    {
        _mapControl.MapSectors = state.Sectors;
        _mapControl.MapBeacons = state.Beacons;
        _mapControl.MapPings = state.Pings;

        // Set the grid reference for walls rendering
        var entManager = IoCManager.Resolve<IEntityManager>();
        _mapControl.MapUid = state.MapUid != null ? entManager.GetEntity(state.MapUid.Value) : null;
        _mapControl.Owner = _mapControl.MapUid;
    }
}
