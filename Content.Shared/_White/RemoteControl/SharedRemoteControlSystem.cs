


using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.BUIStates;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.MouseRotator;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._White.RemoteControl;

public abstract class SharedRemoteControlSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlConsoleUpdateAimDirectionMessage>(OnConsoleMouseMoveBuiMessage);
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlConsoleMouseClickMessage>(OnConsoleMouseClickBuiMessage);
        SubscribeLocalEvent<RemoteControlConsoleComponent, BoundUIOpenedEvent>(OnUiOpen);
        SubscribeLocalEvent<RemoteControlConsoleComponent, BoundUIClosedEvent>(OnUiClosed);

        SubscribeLocalEvent<RemoteControllableComponent, GetVerbsEvent<AlternativeVerb>>(OnTurretGetAltVerbs);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RemoteControlConsoleComponent>();
        while (query.MoveNext(out var consoleUid, out var consoleComp))
        {
            ProcessConsole(consoleUid, consoleComp, frameTime);
        }
    }

    protected virtual void ProcessConsole(EntityUid consoleUid, RemoteControlConsoleComponent consoleComp, float frameTime)
    {
        // if aimdir is null, we haven't even touched the mouse yet
        // no need to handle rotation or shooting
        if (consoleComp.CurrentAimDirection is not Angle aimdir ||
            consoleComp.CurrentTurret is not EntityUid turretUid)
            return;

        if (!TryComp<RemoteControllableComponent>(turretUid, out var turret))
            return;
        var turretXform = Transform(turretUid);

        // for some reason this check can fail when an entity is entering player's pvs range.
        if (TryComp<TransformComponent>(turretXform.ParentUid, out var turretParentXform))
        {
            _rotate.TryRotateTo(
                turretUid,
                _transform.GetWorldRotation(turretParentXform) + aimdir,
                frameTime,
                turret.AngleTolerance,
                turret.RotationSpeed,
                turretXform);
        }
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

        if (!consoleComp.Shooting)
            return;

        if (!_gun.TryGetGun(turretUid, out var gunUid, out var gunComp))
            return;

        gunComp.Target = consoleComp.CurrentAimTarget;
        // null arg instead of proper EntityCoordinates results in the entity shooting only directly forwards.
        // This is not a problem for turrets, but for something like a remote controlled robot this could result in unwanted behaviour.
        // TODO: fix
        _gun.AttemptShoot(turretUid, gunUid, gunComp, null, false);
    }

    private void OnConsoleMouseMoveBuiMessage(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlConsoleUpdateAimDirectionMessage args)
    {
        comp.CurrentAimDirection = args.NewAimDirection;
        Dirty(uid, comp);

        comp.CurrentAimTarget = GetEntity(args.AimTarget);
    }

    private void OnConsoleMouseClickBuiMessage(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlConsoleMouseClickMessage args)
    {
        comp.Shooting = args.Down;
        Dirty(uid, comp);
    }

    protected virtual void OnUiOpen(EntityUid uid, RemoteControlConsoleComponent comp, BoundUIOpenedEvent args) { }

    protected virtual void OnUiClosed(EntityUid uid, RemoteControlConsoleComponent comp, BoundUIClosedEvent args)
    {
        comp.CurrentAimDirection = null;
        comp.Shooting = false;
        Dirty(uid, comp);
    }

    private void OnTurretGetAltVerbs(EntityUid uid, RemoteControllableComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (_lock.IsLocked(uid) ||
            !args.CanInteract ||
            !args.CanAccess ||
            _actor.GetSession(args.User) is not ICommonSession player)
            return;

        args.Verbs.Add(new()
        {
            Text = Loc.GetString("ship-turret-change-id-verb"),
            //Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename.png")),
            Act = () =>
            {
                // do nothing on client except existing
                // this way there is no delay before the rename verb appears in the context menu
                // the fact that i have to do this manually is sad
                if (_net.IsClient)
                    return;
                OnRenameVerb(player, uid, comp);
            },
            Message = Loc.GetString("ship-turret-change-id-verb-description"),
            //Priority = (int) TricksVerbPriorities.Rename,
        });
    }

    protected virtual void OnRenameVerb(ICommonSession player, EntityUid target, RemoteControllableComponent comp) {}
}