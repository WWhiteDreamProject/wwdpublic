using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Containers;

/// <summary>
/// Applies / removes an entity prototype from a child entity when it's inserted into a container.
/// </summary>
public sealed class ContainerCompSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // WD EDIT

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContainerCompComponent, EntInsertedIntoContainerMessage>(OnConInsert);
        SubscribeLocalEvent<ContainerCompComponent, EntRemovedFromContainerMessage>(OnConRemove);
    }

    private void OnConRemove(Entity<ContainerCompComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.Container || _timing.ApplyingState) // WD EDIT
            return;

        // WD EDIT START
        if (_proto.TryIndex(ent.Comp.Proto, out var entProto))
        {
            EntityManager.RemoveComponents(args.Entity, entProto.Components);
        }
        // WD EDIT END
    }

    private void OnConInsert(Entity<ContainerCompComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.Container || _timing.ApplyingState) // WD EDIT
            return;

        if (_proto.TryIndex(ent.Comp.Proto, out var entProto))
        {
            EntityManager.AddComponents(args.Entity, entProto.Components);
        }
    }
}
