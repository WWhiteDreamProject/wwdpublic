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
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using RadarConsoleWindow = Content.Client.Shuttles.UI.RadarConsoleWindow;

namespace Content.Client._White.NavalTurretConsole.UI;

[UsedImplicitly]
public sealed class NavalTurretConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    private SharedTransformSystem _transform = default!;
    private ActionBlockerSystem _actionBlocker = default!;

    [ViewVariables]
    private NavalTurretConsoleWindow? _window;
    private Font _font = default!;
    private Entity<TransformComponent>? _currentTurret;

    public bool Shooting = false;
    public int Shitass = 0;
    public Vector2 Aimpoint = Vector2.Zero;
    
    public NavalTurretConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _font = new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/_White/LCD14/LCD14.otf"), 16);

        _actionBlocker = EntMan.System<ActionBlockerSystem>();
        _window = this.CreateWindow<NavalTurretConsoleWindow>();
        _transform = EntMan.System<SharedTransformSystem>();
        _window.RadarScreen.OnMouseMove += OnMouseMove;
        _window.RadarScreen.OnRadarClick += OnMouseClick;
        _window.RadarScreen.OnMouseExited += (_) => Shooting = false;
        _window.RadarScreen.DrawAfterFoV += DrawTurretIndicator;
        _window.RadarScreen.DrawAfterFoV += DrawAimpoint;
        _window.RadarScreen.SetConsole(Owner);
        _window.RadarScreen.DrawTop += DrawShitass;
    }


    private void DrawShitass(DrawingHandleScreen handle, UIBox2 whatever, Matrix3x2 ass, Matrix3x2 blast, Matrix3x2 usa)
    {
        handle.DrawString(_font, Vector2.Zero, $"Shitass: \"{Shitass}\"\n{Aimpoint}", Color.Green);
    }
    private void OnMouseMove(EntityCoordinates coordinates)
    {
        var angle = EntMan.GetComponent<TransformComponent>(coordinates.EntityId).LocalRotation + MathF.PI;
        //var angle = MathF.PI;
        var aimpoint = angle.RotateVec(coordinates.Position);
        Aimpoint = aimpoint;
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
        SendPredictedMessage(new NavalTurretConsoleMouseClickMessage(EntMan.GetNetCoordinates(coordinates), down));
    }

    protected override void UpdateState(BoundUserInterfaceState _state)
    {
        base.UpdateState(_state);
        if (_state is not NavalTurretConsoleBuiState state)
            return;

        var turret = EntMan.GetEntity(state.CurrentTurret);
        RebuildTurretSelection(state.LinkedTurrets, turret);
        _currentTurret = turret is null ? null : (turret, EntMan.GetComponent<TransformComponent>(turret));
        //_window?.SetError(cState.Error);
        _window?.UpdateState(state.RadarState);

    }

    private void RebuildTurretSelection(List<NetEntity> netEnts, EntityUid? currentEnt)
    {
        _window!.TurretSelection.DisposeAllChildren();
        foreach (var netUid in netEnts)
        {
            var uid = EntMan.GetEntity(netUid);
            var button = new Button();
            if(EntMan.TryGetComponent<NavalTurretComponent>(uid, out var turret))
                button.Text = turret.Name;
            else
                button.Text = $"??? ({uid})";
            button.OnPressed += (_) =>
            {
                SendMessage(new NavalTurretConsoleTurretSelectedBuiMessage(uid == currentEnt ? null : netUid));
                //SendPredictedMessage(new NavalTurretConsoleTurretSelectedBuiMessage(netEnt));
            };
            _window!.TurretSelection.AddChild(button);
        }
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
        if(_currentTurret is not {} curTurret)
            return;

        handle.DrawCircle(curTurret.Comp.LocalPosition, 4, Color.OrangeRed, false);
    }
}
