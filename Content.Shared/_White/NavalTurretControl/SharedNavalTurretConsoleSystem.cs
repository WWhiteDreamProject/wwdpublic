


using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.NavalTurretControl.BUIStates;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.MouseRotator;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._White.NavalTurretControl;

public abstract class SharedNavalTurretConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ActorSystem _actor = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NavalTurretConsoleComponent, NavalTurretConsoleUpdateAimpointMessage>(OnConsoleMouseMoveBuiMessage);
        SubscribeLocalEvent<NavalTurretConsoleComponent, NavalTurretConsoleMouseClickMessage>(OnConsoleMouseClickBuiMessage);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NavalTurretConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var consoleComp, out var xform))
        {
            ProcessConsole(consoleComp, xform, frameTime);
        }
    }

    protected void ProcessConsole(NavalTurretConsoleComponent console, TransformComponent xform, float frameTime)
    {
        // if aimpoint is null, we haven't even touched the mouse yet
        // no need to handle rotation or shooting
        if (console.CurrentAimpoint is not Vector2 aimpoint ||
            console.CurrentTurret is not EntityUid turretUid)
            return;

        if (!TryComp<NavalTurretComponent>(turretUid, out var turret))
            return;

        var turretXform = Transform(turretUid);
        _rotate.TryRotateToCoordinates(
            turretUid,
            new EntityCoordinates(turretXform.ParentUid, turretXform.LocalPosition + aimpoint),
            frameTime,
            turret.AngleTolerance,
            turret.RotationSpeed,
            turretXform);

        //if (_rotate.TryRotateToCoordinates(
        //    turretUid,
        //    _transform.GetWorldPosition(turretXform) + aimpoint,
        //    frameTime,
        //    turret.AngleTolerance,
        //    turret.RotationSpeed,
        //    turretXform))
        //{
        //    turret.CurrentAimpoint = null;
        //    Dirty(uid, turret);
        //}

        if (!console.Shooting)
            return;

        if (!_gun.TryGetGun(turretUid, out var gunUid, out var gun))
            return;

        var rot = -xform.LocalRotation;
        _gun.AttemptShoot(turretUid, gunUid, gun, new(turretUid, rot.RotateVec(aimpoint)), false);
    }

    private void OnConsoleMouseMoveBuiMessage(EntityUid uid, NavalTurretConsoleComponent comp, NavalTurretConsoleUpdateAimpointMessage args)
    {
        comp.CurrentAimpoint = args.NewAimpoint;
        Dirty(uid, comp);
    }

    private void OnConsoleMouseClickBuiMessage(EntityUid uid, NavalTurretConsoleComponent comp, NavalTurretConsoleMouseClickMessage args)
    {
        comp.Shooting = args.Down;
        Dirty(uid, comp);
    }


//    private bool GetTurret(Entity<NavalTurretConsoleComponent> consoleEnt, ICommonSession? session, [NotNullWhen(true)] out Entity<NavalTurretComponent>? entity)
//    {
//        var console = consoleEnt.Comp;
//        if (console.CurrentTurret is not EntityUid turretUid)
//        {
//            Log.Error($"Client {session?.ToString() ?? "[unknown]"} attempted to use {ToPrettyString(consoleEnt)} as gunnery console despite it not being connected to a turret.");
//            DebugTools.Assert(false);
//            entity = null;
//            return false;
//        }
//
//        if (!TryComp<NavalTurretComponent>(turretUid, out var turret))
//        {
//            Log.Error($"Client {session?.ToString() ?? "[unknown]"} attempted to use {ToPrettyString(consoleEnt)} as gunnery console while it had a non-turret entity selected.");
//            DebugTools.Assert(false);
//            entity = null;
//            return false;
//        }
//        entity = (turretUid, turret);
//        return true;
//    }
}