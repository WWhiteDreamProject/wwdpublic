using Content.Shared._White.Xenomorphs.Plasma.Components;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Events;

namespace Content.Server._White.Xenomorphs.Xenomorph;

public sealed class XenomorphSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphComponent, StartCollideEvent>(OnAlienStartCollide);
        SubscribeLocalEvent<XenomorphComponent, EndCollideEvent>(OnAlienEndCollide);
    }

    private void OnAlienStartCollide(EntityUid uid, XenomorphComponent component, StartCollideEvent args)
    {
        if (component.OnWeed || !HasComp<PlasmaGainModifierComponent>(args.OtherEntity))
            return;

        component.OnWeed = true;
    }

    private void OnAlienEndCollide(EntityUid uid, XenomorphComponent component, EndCollideEvent args)
    {
        if (!component.OnWeed || !HasComp<PlasmaGainModifierComponent>(args.OtherEntity))
            return;

        foreach (var contact in _physics.GetContactingEntities(uid))
        {
            if (contact == args.OtherEntity || !HasComp<PlasmaGainModifierComponent>(contact))
                continue;

            return;
        }

        component.OnWeed = false;
    }
}
