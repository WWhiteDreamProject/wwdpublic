


using System.Numerics;
using Content.Shared._White.NavalTurretControl;
using Content.Shared.Interaction;
using Content.Shared.MouseRotator;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._White.NavalTurretControl;

public abstract class SharedNavalTurretConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeAllEvent<RequestNavalTurretShootEvent>(OnTurretShootRequest);
        SubscribeAllEvent<RequestNavalTurretStopShootEvent>(OnTurretShootStopRequest);
        SubscribeAllEvent<RequestNavalTurretRotationEvent>(OnTurretRotationRequest);
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

    private void OnTurretShootRequest(RequestNavalTurretShootEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not EntityUid user)
            return;

        var consoleUid = GetEntity(msg.Console);

        if (!_ui.IsUiOpen(consoleUid, NavalTurretConsoleUiKey.Key, user))
        {
            // could be attributed to network fuckery?
            // Log.Error($"Client {args.SenderSession} requested shot from turret entity without opening the linked console's UI. ({ToPrettyString(consoleUid)} is not linked to {ToPrettyString(turretUid)})");
            return;
        }

        if (!TryComp<NavalTurretConsoleComponent>(consoleUid, out var console))
        {
            Log.Error($"Client {args.SenderSession} attempted to use {ToPrettyString(consoleUid)} as gunnery console despite it having no required component.");
            return;
        }

        if (console.LinkedTurret is not EntityUid turretUid)
        {
            Log.Error($"Client {args.SenderSession} attempted to use {ToPrettyString(consoleUid)} as gunnery console despite it not being connected to a turret.");
            return;
        }

        if (!_gun.TryGetGun(turretUid, out var gunUid, out var gun))
            return;

        _gun.AttemptShoot(turretUid, gunUid, gun, GetCoordinates(msg.Coordinates), false);
    }

    private void OnTurretShootStopRequest(RequestNavalTurretStopShootEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not EntityUid user)
            return;

        var consoleUid = GetEntity(msg.Console);

        if (!_ui.IsUiOpen(consoleUid, NavalTurretConsoleUiKey.Key, user))
        {
            // could be attributed to network fuckery?
            // Log.Error($"Client {args.SenderSession} requested shot from turret entity without opening the linked console's UI. ({ToPrettyString(consoleUid)} is not linked to {ToPrettyString(turretUid)})");
            return;
        }

        if (!TryComp<NavalTurretConsoleComponent>(consoleUid, out var console))
        {
            Log.Error($"Client {args.SenderSession} attempted to use {ToPrettyString(consoleUid)} as gunnery console despite it having no required component.");
            return;
        }

        if (console.LinkedTurret is not EntityUid turretUid)
        {
            Log.Error($"Client {args.SenderSession} attempted to use {ToPrettyString(consoleUid)} as gunnery console despite it not being connected to a turret.");
            return;
        }

        if (!_gun.TryGetGun(turretUid, out var gunUid, out var gun))
            return;

        _gun.StopShooting(gunUid, gun);
    }

    private void OnTurretRotationRequest(RequestNavalTurretRotationEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not EntityUid user)
            return;

        var consoleUid = GetEntity(msg.Console);

        if (!_ui.IsUiOpen(consoleUid, NavalTurretConsoleUiKey.Key, user))
        {
            // could be attributed to network fuckery?
            // Log.Error($"Client {args.SenderSession} requested shot from turret entity without opening the linked console's UI. ({ToPrettyString(consoleUid)} is not linked to {ToPrettyString(turretUid)})");
            return;
        }

        if (!TryComp<NavalTurretConsoleComponent>(consoleUid, out var console))
        {
            Log.Error($"Client {args.SenderSession} attempted to use {ToPrettyString(consoleUid)} as gunnery console despite it having no required component.");
            return;
        }

        if (console.LinkedTurret is not EntityUid turretUid)
        {
            Log.Error($"Client {args.SenderSession} attempted to use {ToPrettyString(consoleUid)} as gunnery console despite it not being connected to a turret.");
            return;
        }

        if(!TryComp<NavalTurretComponent>(turretUid, out var turret))
            return; // fuck it, just fail silently at this point

        turret.CurrentAimpoint = msg.RelativeAimpoint;
        Dirty(turretUid, turret);
    }
}