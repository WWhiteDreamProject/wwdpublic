using System.Numerics;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Interaction;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Systems;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.NavalTurretControl.BUIStates;
using Content.Shared._White.Overlays;
using Content.Shared._White.RemoteControl.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DeviceLinking;
using Content.Shared.Lock;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._White.NavalTurretControl;

public sealed partial class NavalTurretControlSystem : SharedNavalTurretConsoleSystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public static ProtoId<SinkPortPrototype> SinkPortId = "ShipGunnerControlSinkPort";
    public static ProtoId<SourcePortPrototype> SourcePortId = "ShipGunnerControlSourcePort";

    public override void Initialize()
    {
        base.Initialize();
    
        InitializeConsole();
        InitializeTarget();

        SubscribeLocalEvent<NavalTurretConsoleComponent, ComponentStartup>(OnConsoleStartup);

    }

    private void OnConsoleStartup(EntityUid uid, NavalTurretConsoleComponent component, ComponentStartup args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, NavalTurretConsoleComponent? comp = null) => UpdateState((uid, null, comp));


    // TODO: handle multiple attempted console uses (reject everyone until the first user closes the bui) 
    private void UpdateState(Entity<UserInterfaceComponent?, NavalTurretConsoleComponent?> entity)
    {
        if (!_uiSystem.HasUi(entity, NavalTurretConsoleUiKey.Key) ||
            !Resolve(entity, ref entity.Comp1) ||
            !Resolve(entity, ref entity.Comp2))
            return;

        if (entity.Comp2.LinkedTurret is not EntityUid turretUid)
        {
            _uiSystem.SetUiState(entity, NavalTurretConsoleUiKey.Key,
                                 new NavalTurretConsoleBuiState(NavalTurretConsoleError.NotConnected));
            return;
        }

        if(!this.IsPowered(entity, EntityManager))
        {
            _uiSystem.SetUiState(entity, NavalTurretConsoleUiKey.Key,
                                 new NavalTurretConsoleBuiState(NavalTurretConsoleError.NotConnected));
            return;
        }

        if(TryComp<MobStateComponent>(entity, out var stateComp) && stateComp.CurrentState != Shared.Mobs.MobState.Alive ||
           TerminatingOrDeleted(turretUid))
        {
            _uiSystem.SetUiState(entity, NavalTurretConsoleUiKey.Key,
                                 new NavalTurretConsoleBuiState(NavalTurretConsoleError.TurretDestroyed));
            return;
        }


        if(!this.IsPowered(turretUid, EntityManager))
        {
            _uiSystem.SetUiState(entity, NavalTurretConsoleUiKey.Key,
                                 new NavalTurretConsoleBuiState(NavalTurretConsoleError.NoPowerTurret));
            return;
        }


        var state = _console.GetNavState(turretUid, new(), new(turretUid, new Vector2(0,0)), 0);
        _uiSystem.SetUiState(entity, NavalTurretConsoleUiKey.Key, new NavalTurretConsoleBuiState(state));
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
