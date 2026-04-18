using System.Numerics;
using Content.Client._White.NavalTurretConsole.UI;
using Content.Client.Shuttles.UI;
using Content.Shared._White.NavalTurretControl;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using RadarConsoleWindow = Content.Client.Shuttles.UI.RadarConsoleWindow;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class NavalTurretConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NavalTurretConsoleWindow? _window;


    private bool _shooting = false;
    public bool Shooting { get => _shooting;
    set
        {
            _shooting = value; if (_window is not null) _window.Button1.Text = value.ToString();
        }
    }
    private EntityCoordinates _aimpoint = EntityCoordinates.Invalid;
    public EntityCoordinates Aimpoint { get => _aimpoint;
    set
        {
            _aimpoint = value; if (_window is not null) _window.Button2.Text = value.ToString();
        }
    }

    public NavalTurretConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NavalTurretConsoleWindow>();
        _window.RadarScreen.OnMouseMove += OnMouseMove;
        _window.RadarScreen.OnRadarClick += OnMouseClick;
        _window.RadarScreen.OnMouseExited += (_) => Shooting = false;
    }

    private void OnMouseMove(EntityCoordinates coordinates)
    {
        Aimpoint = coordinates;
    }

    private void OnMouseClick(EntityCoordinates coordinates, bool down)
    {
        Shooting = down;
        Aimpoint = coordinates;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not NavBoundUserInterfaceState cState)
            return;

        _window?.UpdateState(cState.State);
    }
}
