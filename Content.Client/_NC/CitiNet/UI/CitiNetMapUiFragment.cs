using Content.Shared._NC.CitiNet;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._NC.CitiNet.UI;

/// <summary>
/// Pure C# implementation of the CitiNet Map UI.
/// Avoids XAML build fragilities and provides robust layout control.
/// </summary>
public sealed partial class CitiNetMapUiFragment : BoxContainer
{
    private CitiNetMapControl _map = default!;
    private CheckBox _sectorsCheck = default!;
    private CheckBox _beaconsCheck = default!;
    private CheckBox _geometryCheck = default!;
    private Button _recenterButton = default!;

    public CitiNetMapUiFragment()
    {
        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;
        VerticalExpand = true;
        Margin = new Thickness(4);

        // Sidebar
        var sidebar = new PanelContainer
        {
            StyleClasses = { "LowDivider" },
            MinWidth = 180,
            Margin = new Thickness(0, 0, 4, 0),
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Margin = new Thickness(6),
                    Children =
                    {
                        new Label { Text = "CITINET MAP", FontColorOverride = Color.FromHex("#00f2ff"), Margin = new Thickness(0, 0, 0, 8) },
                        (_sectorsCheck = new CheckBox { Text = "SECTORS", Pressed = true }),
                        (_beaconsCheck = new CheckBox { Text = "POINTS OF INTEREST", Pressed = true }),
                        (_geometryCheck = new CheckBox { Text = "GEOMETRY", Pressed = true }),
                        new Control { VerticalExpand = true },
                        (_recenterButton = new Button { Text = "[ RECENTER ]", StyleClasses = { "OpenRight" } })
                    }
                }
            }
        };

        // Map Viewport
        _map = new CitiNetMapControl
        {
            HorizontalExpand = true,
            VerticalExpand = true
        };

        AddChild(sidebar);
        AddChild(new PanelContainer
        {
            StyleClasses = { "BorderedWindowPanel" },
            HorizontalExpand = true,
            Children = { _map }
        });

        // Events
        _sectorsCheck.OnToggled += args => _map.ShowSectors = args.Pressed;
        _beaconsCheck.OnToggled += args => _map.ClientBeaconsEnabled = args.Pressed;
        _geometryCheck.OnToggled += args => _map.Visible = args.Pressed;
        _recenterButton.OnPressed += _ => _map.Recenter();
    }

    public void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CitiNetMapBoundUserInterfaceState mapState)
            return;

        _map.MapSectors = mapState.Sectors;
        _map.MapBeacons = mapState.Beacons;
        _map.MapPings = mapState.Pings;

        var entManager = IoCManager.Resolve<IEntityManager>();
        _map.MapUid = mapState.MapUid != null ? entManager.GetEntity(mapState.MapUid.Value) : null;
        _map.Owner = _map.MapUid; // Set owner to the same grid for centering purposes
    }
}
