using System.Numerics;
using Content.Client.Shuttles.BUI;
using Content.Shared._White.NavalTurretControl;
using Content.Shared.Coordinates;
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
        if (_player.LocalEntity is not EntityUid playerEntity)
            return;
        
        if(!_timing.IsFirstTimePredicted)
        {
            base.Update(frameTime);
            return;
        }
        var consoles = EntityQueryEnumerator<NavalTurretConsoleComponent, UserInterfaceComponent, TransformComponent>();
        while (consoles.MoveNext(out var consoleUid, out var consoleComp, out var uiComp, out var xform))
        {
            if (consoleComp.LinkedTurret is not EntityUid turretUid ||
               TerminatingOrDeleted(turretUid) ||
               !TryComp<NavalTurretComponent>(turretUid, out var turret))
               continue;

            if (!_ui.TryGetOpenUi<NavalTurretConsoleBoundUserInterface>((consoleUid, uiComp), NavalTurretConsoleUiKey.Key, out var bui))
                continue;

            var aimpoint = bui.Aimpoint;

            // handle turning input first
            if (turret.CurrentAimpoint is not Vector2 curAimpoint ||
               (aimpoint - curAimpoint).Length() > turret.AimpointTolerane)
            {
                RaisePredictiveEvent(new RequestNavalTurretRotationEvent(aimpoint, GetNetEntity(consoleUid)));
            }

            // and finally, handle shooting input
            // actual shooting will be handled on server
            if (bui.Shooting)
                Request(consoleUid, turretUid, aimpoint, bui.Shooting);
        }
        base.Update(frameTime);
    }

    private void Request(EntityUid consoleUid, EntityUid turretUid, Vector2 aimpoint, bool holdingFire)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is null)
            return;

        if (!_gun.TryGetGun(turretUid, out var _, out var gun))
            return;

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
            Coordinates = GetNetCoordinates(new(turretUid, aimpoint)),
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
