using System;
using System.Numerics;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Actions;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Pinpointer;
using Content.Shared.StatusEffect;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._White.Xenomorphs.Systems;


public sealed class AlienJumpSystem : EntitySystem
{
    // Managers
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    // Systems
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlienJumpComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<AlienJumpComponent, AlienJumpActionEvent>(OnJump);
        SubscribeLocalEvent<AlienJumpComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<AlienJumpComponent, StopThrowEvent>(OnStop);
    }

    private void OnInit(EntityUid uid, AlienJumpComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
    }

    private void OnJump(EntityUid uid, AlienJumpComponent comp, AlienJumpActionEvent args)
    {
        args.Handled = true;

        _throwing.TryThrow(uid, args.Target, 15f);
        _appearance.SetData(uid, JumpVisuals.Jumping, true, Comp<AppearanceComponent>(uid));
    }

    private void OnHit(EntityUid uid, AlienJumpComponent comp, ThrowDoHitEvent args)
    {
        var xform = Transform(args.Target);
        var coords = xform.Coordinates.SnapToGrid(EntityManager, _mapMan);
        var tile = coords.GetTileRef(EntityManager, _mapMan);
        if (tile == null)
            return;

        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            _physics.SetBodyStatus(uid, EnsureComp<PhysicsComponent>(uid), BodyStatus.OnGround);
            _stun.TryParalyze(uid, TimeSpan.FromSeconds(4), true);
            return;

        }
        if (HasComp<StandingStateComponent>(args.Target))
        {
            _physics.SetBodyStatus(uid, EnsureComp<PhysicsComponent>(uid), BodyStatus.OnGround);
            _standing.Down(args.Target, false, dropHeldItems:false);
        }
    }

    private void OnStop(EntityUid uid, AlienJumpComponent comp, StopThrowEvent args)
    {
        _appearance.SetData(uid, JumpVisuals.Jumping, false, Comp<AppearanceComponent>(uid));
    }
}
