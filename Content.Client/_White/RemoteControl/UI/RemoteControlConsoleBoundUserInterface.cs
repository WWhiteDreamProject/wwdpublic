using System.Numerics;
using Content.Client._White.RemoteControl.UI;
using Content.Client.Shuttles.UI;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared._White.Guns.ModularTurret;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.BUIStates;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using RadarConsoleWindow = Content.Client.Shuttles.UI.RadarConsoleWindow;

namespace Content.Client._White.RemoteControl.UI;


/// <summary>
/// This can be best described as several unrelated things cobbled together and
/// held in place with copious amounts of high-quality, industrial-strength toilet paper.
/// 
/// </summary>
[UsedImplicitly]
public sealed class RemoteControlConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEyeManager _eye = default!;
    private SharedTransformSystem _transform = default!;
    private ActionBlockerSystem _actionBlocker = default!;

    private GunSystem _gun = default!;

    [ViewVariables] private RemoteControlConsoleWindow? _window;
    private Font _font = default!;

    public bool Shooting = false;
    public bool Locked = false;
    public Angle? AimDirection { get; private set; } = null;
    public MapCoordinates? CameraAimpoint { get; private set; } = null;
    public Entity<RemoteControllableComponent>? CurrentTurret { get; private set; }
    public RemoteControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _font = new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/_White/LCD14/LCD14.otf"), 16);

        _actionBlocker = EntMan.System<ActionBlockerSystem>();
        _transform = EntMan.System<SharedTransformSystem>();
        _gun = EntMan.System<GunSystem>();
        _window = this.CreateWindow<RemoteControlConsoleWindow>();
        _window.HUDHolder.OnMouseMove += OnMouseMove;
        _window.HUDHolder.OnMouseLeftClick += OnLeftClick;
        _window.RadarScreen.DrawAfterFoV += DrawTurretIndicator;
        _window.RadarScreen.DrawAfterFoV += DrawAimDir;
        _window.RadarScreen.SetConsole(Owner);
    }

    private void OnMouseMove(Vector2? ControlLocalPos)
    {
        if (Locked)
            return;
        if (ControlLocalPos is not Vector2 position)
        {
            OnMouseExited();
            return;
        }
        var dir = position / _window!.HUDHolder.Size - new Vector2(0.5f, 0.5f);
        dir.Y *= -1;
        UpdateAimDirection(dir);

        if (_window!.CameraScreen.Visible)
            CameraAimpoint = _window!.CameraScreen.PixelToMap(_window!.CameraScreen.GlobalPixelPosition + position);
    }

    private void OnMouseExited()
    {
        AimDirection = null;
        Shooting = false;
    }

    private void UpdateAimDirection(Vector2 dir)
    {
        DebugTools.Assert(CurrentTurret is not null);
        var xform = EntMan.GetComponent<TransformComponent>(CurrentTurret.Value);
        var locrot = _transform.GetWorldRotation(xform) - xform.LocalRotation;

        var angle = dir.ToWorldAngle();

        AimDirection = angle - locrot - _eye.CurrentEye.Rotation;
        AimDirection = AimDirection.Value.Reduced();
    }

    private void OnLeftClick(bool down)
    {
        if (Locked)
            return;
        SendPredictedMessage(new RemoteControlConsoleMouseClickMessage(down));
    }

    // looks kinda cool, but i could not find a good use for this
    // when you can just move the cursor off the UI window.
    // TODO: uncomment when i come up with a better use for rmb (maybe target painting?)
    //private void OnRadarRightClick(EntityCoordinates coordinates, bool down)
    //{
    //    if (!down)
    //        return;
    //
    //    Locked = !Locked;
    //    AimDirection = null;
    //    if (!Locked)
    //        UpdateAimDirection(coordinates);
    //}

    protected override void UpdateState(BoundUserInterfaceState someState)
    {
        base.UpdateState(someState);
        if (someState is not RemoteControlConsoleBuiState state)
            return;

        var currentTurretUid = EntMan.GetEntity(state.CurrentTurret);
        if (currentTurretUid is not null)
        {
            if (!EntMan.TryGetComponent<RemoteControllableComponent>(currentTurretUid, out var turretComp))
            {
                UiSystem.Log.Error($"For {nameof(RemoteControlConsoleBoundUserInterface)}, the server sent a turret entity that is missing {nameof(RemoteControllableComponent)}.");
                return; // pretend it never happened
            }
            CurrentTurret = (currentTurretUid.Value, turretComp);
        }
        else
            CurrentTurret = null;

        _window!.CurrentTurret = CurrentTurret;

        AimDirection = null;
        RebuildTurretSelection(state.LinkedTurrets, currentTurretUid);

        var minScale = 1f; // arbitrary
        var maxScale = 3f; // todo: make this adjustable from a component
        var eyeComp = EntMan.GetComponentOrNull<EyeComponent>(currentTurretUid);
        _window?.UpdateState(state.RadarState, eyeComp?.Eye, minScale, maxScale, state.VisualMode, state.Error);
    }

    private void RebuildTurretSelection(List<(NetEntity, bool)> netEnts, EntityUid? currentTurretUid)
    {
        _window!.TurretSelection.DisposeAllChildren();
        foreach (var (turretNetUid, turretAvailable) in netEnts)
        {
            var turretUid = EntMan.GetEntity(turretNetUid);
            if (!EntMan.TryGetComponent<RemoteControllableComponent>(turretUid, out var turret))
                continue;

            var button = new Button();
            button.HorizontalAlignment = Control.HAlignment.Stretch;
            button.Text = !string.IsNullOrWhiteSpace(turret.Name) ? turret.Name : GetPlaceholderName(turretUid, turretNetUid);
            if (turretUid == currentTurretUid)
                button.ModulateSelfOverride = Color.DarkGoldenrod;
            else if (!turretAvailable)
                button.Disabled = true;

            button.OnPressed += (_) =>
            {
                SendMessage(new RemoteControlConsoleTurretSelectedBuiMessage(turretUid == currentTurretUid ? null : turretNetUid));
            };

            _window!.TurretSelection.AddChild(button);
        }

        string GetPlaceholderName(EntityUid uid, NetEntity netUid) => $"{EntMan.GetComponent<MetaDataComponent>(uid).EntityName} {netUid}";
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
