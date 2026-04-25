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
    private SharedTransformSystem _transform = default!;
    private ActionBlockerSystem _actionBlocker = default!;

    [ViewVariables] private NavalTurretConsoleWindow? _window;
    private Font _font = default!;

    public bool Shooting = false;
    public bool Locked = false;
    public int Shitass = 0;
    public Angle? AimDirection = null;
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
        _window.RadarScreen.OnRadarLeftClick += OnRadarLeftClick;
        _window.RadarScreen.OnRadarRightClick += OnRadarRightClick;
        _window.RadarScreen.OnMouseExited += (_) => Shooting = false;
        _window.RadarScreen.DrawAfterFoV += DrawTurretIndicator;
        _window.RadarScreen.DrawAfterFoV += DrawAimDir;
        _window.RadarScreen.SetConsole(Owner);
        _window.RadarScreen.DrawTop += DrawShitass;
    }


    private void DrawShitass(DrawingHandleScreen handle, UIBox2 whatever, Matrix3x2 ass, Matrix3x2 blast, Matrix3x2 usa)
    {
        handle.DrawString(_font, Vector2.Zero, Locked ? "CONTROLS LOCKED\nPRESS RIGHT TRIGGER TO UNLOCK" : "", Color.Green);
    }
    private void OnMouseMove(Vector2 controlPos, EntityCoordinates coordinates)
    {
        if (Locked)
            return;
        UpdateAimDirection(coordinates);
    }

    private void UpdateAimDirection(EntityCoordinates coordinates)
    {
        var angle = EntMan.GetComponent<TransformComponent>(coordinates.EntityId).LocalRotation + MathF.PI;

        var unrotatedPos = angle.RotateVec(coordinates.Position);

        //var angle = MathF.PI;
        AimDirection = Angle.FromWorldVec(unrotatedPos);
        AimDirection = AimDirection.Value.Reduced();
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
    private void OnRadarLeftClick(EntityCoordinates coordinates, bool down)
    {
        if (Locked)
            return;
        UpdateAimDirection(coordinates);
        SendPredictedMessage(new NavalTurretConsoleMouseClickMessage(EntMan.GetNetCoordinates(coordinates), down));
    }

    private void OnRadarRightClick(EntityCoordinates coordinates, bool down)
    {
        if (!down)
            return;

        Locked = !Locked;
        AimDirection = null;
        if(!Locked)
            UpdateAimDirection(coordinates);
    }

    protected override void UpdateState(BoundUserInterfaceState _state)
    {
        base.UpdateState(_state);
        if (_state is not NavalTurretConsoleBuiState state)
            return;

        var turret = EntMan.GetEntity(state.CurrentTurret);
        RebuildTurretSelection(state.LinkedTurrets, turret);
        //_window?.SetError(cState.Error);
        _window?.UpdateState(state.RadarState);
        AimDirection = null;
    }

    private void RebuildTurretSelection(List<NetEntity> netEnts, EntityUid? currentEnt)
    {
        _window!.TurretSelection.DisposeAllChildren();
        foreach (var netUid in netEnts)
        {
            var uid = EntMan.GetEntity(netUid);
            var button = new Button();
            if (!EntMan.TryGetComponent<NavalTurretComponent>(uid, out var turret))
                continue;

            button.Text = turret.Name ?? $"{EntMan.ToPrettyString(uid)}";

            if (turret.CurrentConsole is EntityUid curConsole)
            {
                if (curConsole == Owner)
                    button.ModulateSelfOverride = Color.Gold;
                else
                    button.Disabled = true;
            }

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
        handle.DrawCircle(circlePos, scale * _window!.RadarScreen.MinimapScale, Color.DodgerBlue);

        var point1 = Vector2.Transform(new Vector2(1, 0) * scale, ourEntToView);
        var point2 = Vector2.Transform(new Vector2(0, -2) * scale, ourEntToView);
        var point3 = Vector2.Transform(new Vector2(-1, 0) * scale, ourEntToView);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, new Vector2[] { point1, point2, point3 }, Color.DodgerBlue);
    }

    private void DrawAimDir(DrawingHandleScreen handle, UIBox2 controlSize, Matrix3x2 ourEntToWorld, Matrix3x2 shuttleToWorld, Matrix3x2 worldToView)
    {
        // TODO: draw target aim direction as well.
        var lineEndPosLocal = new Vector2(0, -1 / _window!.RadarScreen.MinimapScale * 512);
        var lineEndPos = Vector2.Transform(lineEndPosLocal, ourEntToWorld * worldToView);
        // for some reason, DrawLine uses an different shade of blue when passing Color.DodgerBlue compared to DrawPrimitives.
        // Did not check which one is correct and whether it happens with other colors, since i don't care enough about it. 
        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, new Vector2[] { controlSize.Center, lineEndPos }, Color.DodgerBlue);
    }
}
