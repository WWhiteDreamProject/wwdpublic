using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server.Item;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._White.Melee.Snatch;

public sealed class SnatchOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnatchOnMeleeHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, SnatchOnMeleeHitComponent component, MeleeHitEvent args)
    {
        if (!_itemToggle.IsActivated(uid) || args.HitEntities.Count == 0)
            return;

        var entity = args.HitEntities.First();

        if (entity == uid || !TryComp(entity, out HandsComponent? hands))
            return;

        foreach (var heldEntity in _hands.EnumerateHeld(entity, hands))
        {
            if (!_hands.TryDrop(entity, heldEntity, null, false, false, hands))
                continue;

            _hands.PickupOrDrop(args.User, heldEntity, false);
            break;
        }
    }
}
