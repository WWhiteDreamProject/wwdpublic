using Content.Client.Shuttles.BUI;
using Content.Shared._White.NavalTurretControl;
using Content.Shared.DeviceLinking;
using Content.Shared.MouseRotator;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._White.NavalTurretControl;

public sealed partial class NavalTurretControlSystem : SharedNavalTurretConsoleSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not EntityUid playerEntity ||
            !_timing.IsFirstTimePredicted)
            return;

        var consoles = EntityQueryEnumerator<NavalTurretConsoleComponent, UserInterfaceComponent>();
        while (consoles.MoveNext(out var consoleUid, out var consoleComp, out var uiComp))
        {
            if (consoleComp.LinkedTurret is not EntityUid turretUid ||
               TerminatingOrDeleted(turretUid) ||
               !HasComp<NavalTurretComponent>(turretUid))
               continue;

            if (!_ui.TryGetOpenUi<NavalTurretConsoleBoundUserInterface>((consoleUid, uiComp), NavalTurretConsoleUiKey.Key, out var bui))
                continue;

            if (!bui.Shooting || bui.Aimpoint == EntityCoordinates.Invalid)
                continue;

            DebugTools.Assert(bui.Aimpoint.EntityId == turretUid); // if aimpoint is not relative to the turret, something went wrong

            Request(consoleUid, turretUid, bui.Aimpoint, bui.Shooting);

            if(!TryComp<MouseRotatorComponent>(turretUid, out var rotator))
                return;

            var angle = (_transform.ToMapCoordinates(bui.Aimpoint).Position - _transform.GetWorldPosition(turretUid)).ToWorldAngle();
            var diff = Angle.ShortestDistance(angle, _transform.GetWorldRotation(turretUid));
            if (Math.Abs(diff.Theta) < rotator.AngleTolerance.Theta)
                return;

            if (rotator.GoalRotation != null)
            {
                var goalDiff = Angle.ShortestDistance(angle, rotator.GoalRotation.Value);
                if (Math.Abs(goalDiff.Theta) < rotator.AngleTolerance.Theta)
                    return;
            }
            RaisePredictiveEvent(new RequestNavalTurretRotationEvent(angle));
        }
    }

    private void Request(EntityUid consoleUid, EntityUid turretUid, EntityCoordinates aimpoint, bool holdingFire)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not EntityUid playerEntity)
            return;

        if (!_gun.TryGetGun(turretUid, out var gunUid, out var gun))
        {
            return;
        }

        if (!holdingFire && !gun.BurstActivated)
        {
            if (gun.ShotCounter != 0)
                EntityManager.RaisePredictiveEvent(new RequestNavalTurretStopShootEvent
                {
                    Console = GetNetEntity(consoleUid)
                });
            return;
        }

        if (gun.NextFire > _timing.CurTime)
            return;

        //if (aimpoint.MapId == MapId.Nullspace)
        //{
        //    if (gun.ShotCounter != 0)
        //        EntityManager.RaisePredictiveEvent(new RequestNavalTurretStopShootEvent { Gun = GetNetEntity(gunUid) });
        //    return;
        //}

        Log.Debug($"Sending naval turret shoot request tick {_timing.CurTick} / {_timing.CurTime}");

        EntityManager.RaisePredictiveEvent(new RequestNavalTurretShootEvent
        {
            Console = GetNetEntity(consoleUid),
            Coordinates = GetNetCoordinates(aimpoint),
        });
    }

}



//public sealed class RadarConsoleSystem : EntitySystem
//{
//    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
//    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
//
//    public override void Initialize()
//    {
//        base.Initialize();
//        SubscribeLocalEvent<RadarConsoleComponent, ComponentStartup>(OnRadarStartup);
//    }
//
//    private void OnRadarStartup(EntityUid uid, RadarConsoleComponent component, ComponentStartup args)
//    {
//        UpdateState(uid, component);
//    }
//
//    protected override void UpdateState(EntityUid uid, RadarConsoleComponent component)
//    {
//        var xform = Transform(uid);
//        var onGrid = xform.ParentUid == xform.GridUid;
//        EntityCoordinates? coordinates = onGrid ? xform.Coordinates : null;
//        Angle? angle = onGrid ? xform.LocalRotation : null;
//
//        if (component.FollowEntity)
//        {
//            coordinates = new EntityCoordinates(uid, Vector2.Zero);
//            angle = 0;
//        }
//
//        if (_uiSystem.HasUi(uid, RadarConsoleUiKey.Key))
//        {
//            NavInterfaceState state;
//            var docks = _console.GetAllDocks();
//
//            if (coordinates != null && angle != null)
//            {
//                state = _console.GetNavState(uid, docks, coordinates.Value, angle.Value);
//            }
//            else
//            {
//                state = _console.GetNavState(uid, docks);
//            }
//
//            state.RotateWithEntity = component.RotateWithEntity; // WD EDIT
//
//            _uiSystem.SetUiState(uid, RadarConsoleUiKey.Key, new NavBoundUserInterfaceState(state));
//        }
//    }
//}
