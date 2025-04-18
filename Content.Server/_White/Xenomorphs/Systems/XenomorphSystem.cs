using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class XenomorphSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphComponent, PickupAttemptEvent>(OnPickup);
        SubscribeLocalEvent<XenomorphComponent, PlayerAttachedEvent>(OnTakeRole);

        SubscribeLocalEvent<XenomorphComponent, StartCollideEvent>(OnAlienStartCollide);
        SubscribeLocalEvent<XenomorphComponent, EndCollideEvent>(OnAlienEndCollide);
    }

    private void OnTakeRole(EntityUid uid, XenomorphComponent component, PlayerAttachedEvent args)
    {
        if (string.IsNullOrEmpty(component.GreetingText))
            return;

        _chatMan.DispatchServerMessage(args.Player, Loc.GetString(component.GreetingText));
    }

    private void OnPickup(EntityUid uid, XenomorphComponent component, PickupAttemptEvent args)
    {
        if (_tag.HasTag(args.Item, "AlienItem"))
            return;

        args.Cancel();
        _popup.PopupEntity(Loc.GetString("alien-pickup-item-fail"), uid, uid);
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
