using System.Numerics;
using Content.Client._White.NavalTurretConsole.UI;
using Content.Client.Shuttles.UI;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared._White.Guns.ModularTurret;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.NavalTurretControl.BUIStates;
using Content.Shared.ActionBlocker;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;
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

namespace Content.Client._White.NavalTurretConsole.UI;

[UsedImplicitly]
public sealed class NavalTurretConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _timing = default!;
    private SharedTransformSystem _transform = default!;
    private ActionBlockerSystem _actionBlocker = default!;
    private GunSystem _gun = default!;

    [ViewVariables] private NavalTurretConsoleWindow? _window;
    private Font _font = default!;

    public bool Shooting = false;
    public bool Locked = false;
    public Angle? AimDirection { get; private set; } = null;
    public Entity<NavalTurretComponent>? CurrentTurret { get; private set; }
    public NavalTurretConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _font = new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/_White/LCD14/LCD14.otf"), 16);

        _actionBlocker = EntMan.System<ActionBlockerSystem>();
        _transform = EntMan.System<SharedTransformSystem>();
        _gun = EntMan.System<GunSystem>();
        _window = this.CreateWindow<NavalTurretConsoleWindow>();
        _window.RadarScreen.OnMouseMove += OnMouseMove;
        _window.RadarScreen.OnMouseExited += OnMouseExited;
        _window.RadarScreen.OnRadarLeftClick += OnRadarLeftClick;
        _window.RadarScreen.OnRadarRightClick += OnRadarRightClick;
        _window.RadarScreen.OnMouseExited += (_) => Shooting = false;
        _window.RadarScreen.DrawAfterFoV += DrawTurretIndicator;
        _window.RadarScreen.DrawAfterFoV += DrawAimDir;
        _window.RadarScreen.SetConsole(Owner);
        _window.RadarScreen.DrawTop += DrawTop;
    }

    private bool IsTimePeriod(double active, double inactive) => _timing.CurTime.TotalSeconds % (active + inactive) <= active;
    private void DrawTop(DrawingHandleScreen handle, UIBox2 controlSize, Matrix3x2 ourEntToWorld, Matrix3x2 shuttleToWorld, Matrix3x2 worldToView)
    {
        //if (Locked && IsTimePeriod(0.5, 0.5))
        //    handle.DrawString(_font, Vector2.Zero, "CONTROLS LOCKED\nPRESS RIGHT TRIGGER TO UNLOCK", Color.Green);

        if (CurrentTurret is null)
            return;

        if (!_gun.TryGetGun(CurrentTurret.Value, out var gunUid, out var gunComp))
            return;

        DrawWeaponName(handle, gunUid);
        DrawCooldown(handle, controlSize, gunComp);
        DrawAmmoCounter(handle, controlSize, gunUid);

    }

    private void DrawWeaponName(DrawingHandleScreen handle, EntityUid gunUid)
    {
        if (!EntMan.TryGetComponent<ModularTurretWeaponComponent>(gunUid, out var modComp))
            return;
        var name = modComp.Name;
        if (name is null)
            name = "UNKNOWN";
        // TODO: uncomment after merging the lobby vanity text from EE, it has a funny RANDOM() func for localization
        //else if (Loc.TryGetString(name, out var newName))
        //    name = newName;

        handle.DrawString(_font, Vector2.Zero, $"WEP: {name}", Color.AntiqueWhite);
    }

    private void DrawCooldown(DrawingHandleScreen handle, UIBox2 controlSize, GunComponent gunComp)
    {
        string cooldownString;
        if (gunComp.NextFire >= _timing.CurTime)
            cooldownString = $"{(gunComp.NextFire - _timing.CurTime).TotalSeconds: 0.00}s";
        else
            cooldownString = "RDY";
        handle.DrawString(_font, controlSize.Size - handle.GetDimensions(_font, cooldownString, 1f), cooldownString, gunComp.NextFire >= _timing.CurTime ? Color.Yellow : Color.Green);
    }

    private void DrawAmmoCounter(DrawingHandleScreen handle, UIBox2 controlSize, EntityUid gunUid)
    {
        if (!_gun.TryGetBatteryCharges(gunUid, out var shots, out var capacity))
        {
            var noBatteryString = "BAT: N/A";
            var noBatteryStringHeight = handle.GetDimensions(_font, noBatteryString, 1f).Y;
            handle.DrawString(_font, new Vector2(0, controlSize.Height - noBatteryStringHeight), noBatteryString, Color.AntiqueWhite);
            return;
        }

        var chargePercent = (float) shots / capacity * 100;
        var len = (int) MathF.Floor(MathF.Log10(capacity) + 1);
        var shotCounter = $"{shots.ToString($"D{len}")}/{capacity}";
        var warn = chargePercent < 25 && IsTimePeriod(0.2, 0.2) ? "WARN" : string.Empty;
        var batteryString = $"BAT:{(chargePercent < 100 ? " " : "")}{chargePercent: 00.00}% ({shotCounter}) {warn}";
        var batteryStringColor = chargePercent switch
        {
            >= 75 => Color.Green,
            >= 50 => Color.Yellow,
            >= 25 => Color.OrangeRed,
            >= 0 => Color.Red,
            _ => Color.Green
        };
        var batteryStringHeight = handle.GetDimensions(_font, batteryString, 1f).Y;
        handle.DrawString(_font, new Vector2(0, controlSize.Height - batteryStringHeight), batteryString, batteryStringColor);
    }

    private void OnMouseMove(EntityCoordinates coordinates)
    {
        if (Locked)
            return;
        UpdateAimDirection(coordinates);
    }

    private void OnMouseExited(GUIMouseHoverEventArgs args)
    {
        AimDirection = null;
    }

    private void UpdateAimDirection(EntityCoordinates coordinates)
    {
        var angle = EntMan.GetComponent<TransformComponent>(coordinates.EntityId).LocalRotation + MathF.PI;

        var unrotatedPos = angle.RotateVec(coordinates.Position);

        //var angle = MathF.PI;
        AimDirection = Angle.FromWorldVec(unrotatedPos);
        AimDirection = AimDirection.Value.Reduced();
    }

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

        // looks kinda cool, but i could not find a good use for this
        // when you can just move the cursor off the UI window.
        //Locked = !Locked;
        //AimDirection = null;
        //if (!Locked)
        //    UpdateAimDirection(coordinates);
    }

    protected override void UpdateState(BoundUserInterfaceState someState)
    {
        base.UpdateState(someState);
        if (someState is not NavalTurretConsoleBuiState state)
            return;

        var turretUid = EntMan.GetEntity(state.CurrentTurret);
        if (turretUid is not null)
        {
            if (!EntMan.TryGetComponent<NavalTurretComponent>(turretUid, out var turretComp))
            {
                UiSystem.Log.Error($"For {nameof(NavalTurretConsoleBoundUserInterface)}, the server sent a turret entity that is missing {nameof(NavalTurretComponent)}.");
                return; // pretend it never happened
            }
            CurrentTurret = (turretUid.Value, turretComp);
        }
        else
            CurrentTurret = null;

        AimDirection = null;
        RebuildTurretSelection(state.LinkedTurrets, turretUid);
        _window?.SetError(state.Error);
        _window?.UpdateState(state.RadarState);
    }

    private void RebuildTurretSelection(List<(NetEntity, bool)> netEnts, EntityUid? currentTurretUid)
    {
        _window!.TurretSelection.DisposeAllChildren();
        foreach (var (turretNetUid, turretAvailable) in netEnts)
        {
            var turretUid = EntMan.GetEntity(turretNetUid);
            var button = new Button();
            if (!EntMan.TryGetComponent<NavalTurretComponent>(turretUid, out var turret))
                continue;

            var button = new Button();
            button.HorizontalAlignment = Control.HAlignment.Stretch;
            button.Text = !string.IsNullOrWhiteSpace(turret.Name) ? turret.Name : EntMan.ToPrettyString(turretUid); // todo: replace ToPrettyString with something less debug-flavoured
            if (turretUid == currentTurretUid)
                button.ModulateSelfOverride = Color.DarkGoldenrod;
            else if (!turretAvailable)
                button.Disabled = true;

            button.OnPressed += (_) =>
            {
                SendMessage(new NavalTurretConsoleTurretSelectedBuiMessage(turretUid == currentTurretUid ? null : turretNetUid));
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
