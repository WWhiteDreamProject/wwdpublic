using System.Numerics;
using Content.Client._White.NavalTurretConsole.UI;
using Content.Client.Shuttles.UI;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.NavalTurretControl.BUIStates;
using Content.Shared.ActionBlocker;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using RadarConsoleWindow = Content.Client.Shuttles.UI.RadarConsoleWindow;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class NavalTurretConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    private SharedTransformSystem _transform = default!;
    private ActionBlockerSystem _actionBlocker = default!;

    [ViewVariables]
    private NavalTurretConsoleWindow? _window;

    private bool _shooting = false;
    public bool Shooting { get => _shooting;
    set
        {
            _shooting = value; if (_window is not null) _window.Button1.Text = value.ToString();
        }
    }
    private Vector2 _aimpoint = Vector2.Zero;
    public Vector2 Aimpoint { get => _aimpoint;
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
        _transform = EntMan.System<SharedTransformSystem>();
        _window.RadarScreen.OnMouseMove += OnMouseMove;
        _window.RadarScreen.OnRadarClick += OnMouseClick;
        _window.RadarScreen.OnMouseExited += (_) => Shooting = false;
        _window.RadarScreen.DrawAfterFoV += DrawTurretIndicator;
        _window.RadarScreen.DrawAfterFoV += DrawAimpoint;
        _window.RadarScreen.SetConsole(Owner);
        _actionBlocker = EntMan.System<ActionBlockerSystem>();
    }

    private bool CanInteract(EntityUid? user) => user is EntityUid && _actionBlocker.CanInteract(user.Value, Owner);

    private void OnMouseMove(EntityCoordinates coordinates)
    {
        if(!CanInteract(_player.LocalEntity))
            return;
        var angle = EntMan.GetComponent<TransformComponent>(Owner).LocalRotation + MathF.PI;
        var aimpointAdjusted = (-angle).RotateVec(coordinates.Position);
        Aimpoint = aimpointAdjusted;
    }

    // TODO: invoke clientside bui messages on mouseclick and move
    //       i.e:
    //       NavalConsoleMouseMoveBuiMessage - raised and handled by the client
    //                                         will implicitly cut off any interaction that is not supposed to happen
    //                                         (such as user issuing firing commands while being unconscious)
    //          raised in OnMouseMove with entitycoords or pseudolocal coords
    //
    //      Handle this message in the very next method that would have the current OnMouseMove logic
    //      if it gets called, it means that whatever requirements set by UISystem for the user
    //      to be able to interact with us are (still being) met
    //      
    //      And then do the same with OnMouseClick.
    //      And the same with OnTurretSelection (if implemented)
    private void OnMouseClick(EntityCoordinates coordinates, bool down)
    {
        if(!CanInteract(_player.LocalEntity))
        {
            Shooting = false;
            return;
        }
        Shooting = down;
        var angle = EntMan.GetComponent<TransformComponent>(Owner).LocalRotation + MathF.PI;
        var aimpointAdjusted = (-angle).RotateVec(coordinates.Position);
        Aimpoint = aimpointAdjusted;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not NavalTurretConsoleBuiState cState)
            return;

        _window?.SetError(cState.Error);
        _window?.UpdateState(cState.State);
    }


    private void DrawTurretIndicator(DrawingHandleScreen handle, UIBox2 controlSize, Matrix3x2 ourEntToWorld, Matrix3x2 shuttleToWorld, Matrix3x2 worldToView)
    {
        var ourEntToView = ourEntToWorld * worldToView;
        
        const float scale = 2f;
        var circlePos = Vector2.Transform(Vector2.Zero, ourEntToView);
        var point1 = Vector2.Transform(new Vector2(1, 0) * scale, ourEntToView);
        var point2 = Vector2.Transform(new Vector2(0, -2) * scale, ourEntToView);
        var point3 = Vector2.Transform(new Vector2(-1, 0) * scale, ourEntToView);
        var aimpointDist = Aimpoint.Length();
        var linePoint1 = Vector2.Transform(new Vector2(0, -1) * aimpointDist*0.9f, ourEntToView);
        var linePoint2 = Vector2.Transform(new Vector2(0, -1) * aimpointDist*1.1f, ourEntToView);


        handle.DrawCircle(circlePos, scale * _window!.RadarScreen.MinimapScale, Color.DodgerBlue);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, new Vector2[]{ point1, point2, point3 }, Color.DodgerBlue);
        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, new Vector2[]{ linePoint1, linePoint2 }, Color.DodgerBlue);
    }

    private void DrawAimpoint(DrawingHandleScreen handle, UIBox2 controlSize, Matrix3x2 ourEntToWorld, Matrix3x2 shuttleToWorld, Matrix3x2 worldToView)
    {
        var aimpointWorld = ourEntToWorld.Translation + Aimpoint;
        handle.DrawCircle(Vector2.Transform(aimpointWorld, worldToView), 4, Color.OrangeRed, false);
    }
}
