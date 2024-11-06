using Content.Shared.Body.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.OfferItem;
using Robust.Shared.Containers;
using Robust.Shared.Spawners;

namespace Content.Server._White.DespawnOnLandItem;

public sealed class DespawnOnLandItemSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DespawnOnLandItemComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<DespawnOnLandItemComponent, HandedEvent>(OnHanded);
        SubscribeLocalEvent<DespawnOnLandItemComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
    }

    private void OnDrop(EntityUid uid, DespawnOnLandItemComponent component, DroppedEvent args)
    {
        EnsureComp<TimedDespawnComponent>(uid).Lifetime = component.TimeDespawnOnLand;
    }

    private void OnHanded(EntityUid uid, DespawnOnLandItemComponent component, HandedEvent args)
    {
        EnsureComp<TimedDespawnComponent>(uid).Lifetime = component.TimeDespawnOnLand;
    }

    private void OnInsert(EntityUid uid, DespawnOnLandItemComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (!HasComp<BodyComponent>(args.Container.Owner))
            EnsureComp<TimedDespawnComponent>(uid).Lifetime = component.TimeDespawnOnLand;
    }
}
