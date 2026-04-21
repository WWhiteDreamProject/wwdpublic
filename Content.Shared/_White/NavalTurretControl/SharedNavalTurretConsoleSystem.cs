


using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
        SubscribeLocalEvent<NavalTurretConsoleComponent, RequestNavalTurretShootBuiMessage>(OnTurretShootBuiMessage);
        SubscribeLocalEvent<NavalTurretConsoleComponent, RequestNavalTurretStopShootBuiMessage>(OnTurretShootStopBuiMessage);
        SubscribeLocalEvent<NavalTurretConsoleComponent, RequestNavalTurretUpdateAimpointBuiMessage>(OnTurretUpdateAimpointBuiMessage);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NavalTurretComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var turret, out var xform))
        {
            HandleRotation(uid, turret, xform, frameTime);
        }
    }

    protected void HandleRotation(EntityUid uid, NavalTurretComponent turret, TransformComponent? xform, float frameTime)
    {
        DebugTools.AssertOwner(uid, turret);
        DebugTools.AssertOwner(uid, xform);

        if (!Resolve(uid, ref xform))
            return;

        if (turret.CurrentAimpoint is not Vector2 aimpoint)
            return;

        if (_rotate.TryRotateToCoordinates(
                uid,
                _transform.GetWorldPosition(xform) + aimpoint,
                frameTime,
                turret.AngleTolerance,
                turret.RotationSpeed,
                xform))
        {
            // Stop rotating if we finished
            turret.CurrentAimpoint = null;
            Dirty(uid, turret);
        }
    }

    private void OnTurretShootBuiMessage(EntityUid uid, NavalTurretConsoleComponent comp, RequestNavalTurretShootBuiMessage args)
    {
        var user = args.Actor;
        var session = _actor.GetSession(user);

        var consoleUid = GetEntity(args.Console);

        if (!GetTurret((uid, comp), session, out var turretEnt))
            return;

        if (!_gun.TryGetGun(turretEnt.Value, out var gunUid, out var gun))
            return;

        // the aimpoint we receive is relative to the turret entity as if it had
        // LocalRotation of zero. Therefore, we cannot pass it to AttemptShoot()
        // without transforming it into proper EntityCoordinates.
        var rot = -Transform(turretEnt.Value).LocalRotation;
        _gun.AttemptShoot(turretEnt.Value, gunUid, gun, new(consoleUid, rot.RotateVec(args.RelativeAimpoint)), false);
    }

    private void OnTurretShootStopBuiMessage(EntityUid uid, NavalTurretConsoleComponent comp, RequestNavalTurretStopShootBuiMessage args)
    {
        var user = args.Actor;
        var session = _actor.GetSession(user);

        if (!GetTurret((uid, comp), session, out var turretEnt))
            return;

        if (!_gun.TryGetGun(turretEnt.Value, out var gunUid, out var gun))
            return;

        _gun.StopShooting(gunUid, gun);
    }

    private void OnTurretUpdateAimpointBuiMessage(EntityUid uid, NavalTurretConsoleComponent comp, RequestNavalTurretUpdateAimpointBuiMessage args)
    {
        var user = args.Actor;
        var session = _actor.GetSession(user);

        if (!GetTurret((uid, comp), session, out var turretEnt))
            return;

        turretEnt.Value.Comp.CurrentAimpoint = args.RelativeAimpoint;
        Dirty(turretEnt.Value);
    }

    private bool GetTurret(Entity<NavalTurretConsoleComponent> consoleEnt, ICommonSession? session, [NotNullWhen(true)] out Entity<NavalTurretComponent>? entity)
    {
        var console = consoleEnt.Comp;
        if (console.LinkedTurret is not EntityUid turretUid)
        {
            Log.Error($"Client {session?.ToString() ?? "[unknown]" } attempted to use {ToPrettyString(consoleEnt)} as gunnery console despite it not being connected to a turret.");
            entity = null;
            return false;
        }

        if (!TryComp<NavalTurretComponent>(turretUid, out var turret))
        {
            Log.Error($"Client {session?.ToString() ?? "[unknown]"} attempted to use {ToPrettyString(consoleEnt)} as gunnery console while it was connected to a non-turret entity.");
            entity = null;
            return false;
        }
        entity = (turretUid, turret);
        return true;
    }
}