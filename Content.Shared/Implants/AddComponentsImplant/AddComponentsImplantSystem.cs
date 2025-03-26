using Robust.Shared.Containers;
using Content.Shared.Implants;

namespace Content.Shared.Implants.AddComponentsImplant;

public sealed class AddComponentsImplantSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddComponentsImplantComponent, SubdermalImplantInserted>(OnImplantImplantedEvent); // WD EDIT
        SubscribeLocalEvent<AddComponentsImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    private void OnImplantImplantedEvent(Entity<AddComponentsImplantComponent> ent, ref SubdermalImplantInserted args) // WD EDIT
    {
        foreach (var component in ent.Comp.ComponentsToAdd)
        {
            // Don't add the component if it already exists
            if (EntityManager.HasComponent(args.Target, _factory.GetComponent(component.Key).GetType())) // WD EDIT
                continue;

            EntityManager.AddComponent(args.Target, component.Value); // WD EDIT
            ent.Comp.AddedComponents.Add(component.Key, component.Value);
        }
    }

    private void OnRemove(Entity<AddComponentsImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        EntityManager.RemoveComponents(args.Container.Owner, ent.Comp.AddedComponents);

        // Clear the list so the implant can be reused.
        ent.Comp.AddedComponents.Clear();
    }
}
