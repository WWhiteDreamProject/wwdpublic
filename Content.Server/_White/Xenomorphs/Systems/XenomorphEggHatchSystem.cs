using Content.Server._White.Xenomorphs.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Inventory;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class XenomorphEggHatchSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphEggHatchComponent, InteractHandEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, XenomorphEggHatchComponent component, InteractHandEvent args)
    {
        _polymorph.PolymorphEntity(uid, component.PolymorphPrototype);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<XenomorphEggHatchComponent>();
        while (query.MoveNext(out var uid, out var alienEgg))
        {
            bool hasMaskEntityNearby = false;
            bool hasGhostEntityNearby = false;

            foreach (var entity in _lookup.GetEntitiesInRange(uid, alienEgg.ActivationRange))
            {
                hasMaskEntityNearby = _inventory.HasSlot(entity, "mask");
                hasGhostEntityNearby = EntityManager.HasComponent<GhostComponent>(entity);
            }

            if (hasMaskEntityNearby && !hasGhostEntityNearby)
            {
                _polymorph.PolymorphEntity(uid, alienEgg.PolymorphPrototype);
            }
        }
    }
}
