using System.Linq;
using Content.Server.Stealth;
using Content.Shared._Friday31.Slenderman;
using Content.Shared.Actions;
using Content.Shared.Physics;
using Content.Shared.Stealth.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Friday31.Slenderman;

public sealed class SlendermanShadowWalkSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlendermanShadowWalkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlendermanShadowWalkComponent, SlendermanShadowWalkEvent>(OnShadowWalk);
    }

    private void OnMapInit(EntityUid uid, SlendermanShadowWalkComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action);
        _actions.SetToggled(component.ActionEntity, component.InShadow);
    }

    private void OnShadowWalk(EntityUid uid, SlendermanShadowWalkComponent component, SlendermanShadowWalkEvent args)
    {
        if (args.Handled)
            return;

        component.InShadow = !component.InShadow;

        if (component.InShadow)
        {
            EnterShadow(uid, component);
        }
        else
        {
            ExitShadow(uid, component);
        }
        _actions.SetToggled(component.ActionEntity, component.InShadow);

        Dirty(uid, component);
        args.Handled = true;
    }

    private void EnterShadow(EntityUid uid, SlendermanShadowWalkComponent component)
    {
        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, 0f, stealth);

        if (!TryComp<FixturesComponent>(uid, out var fixtures) || fixtures.FixtureCount < 1)
            return;

        var fixture = fixtures.Fixtures.First();
        _physics.SetHard(uid, fixture.Value, false, fixtures);
        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int)CollisionGroup.GhostImpassable, fixtures);
        _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, 0, fixtures);

        _audio.PlayPvs("/Audio/Effects/teleport_arrival.ogg", uid);
    }

    private void ExitShadow(EntityUid uid, SlendermanShadowWalkComponent component)
    {
        if (TryComp<StealthComponent>(uid, out var stealth))
        {
            RemComp<StealthComponent>(uid);
        }

        if (!TryComp<FixturesComponent>(uid, out var fixtures) || fixtures.FixtureCount < 1)
            return;

        var fixture = fixtures.Fixtures.First();
        _physics.SetHard(uid, fixture.Value, true, fixtures);
        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int)CollisionGroup.MobMask, fixtures);
        _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int)CollisionGroup.MobLayer, fixtures);

        _audio.PlayPvs("/Audio/Effects/teleport_departure.ogg", uid);
    }
}
