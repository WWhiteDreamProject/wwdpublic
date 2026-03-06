using System.Linq;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Prototypes;
using Content.Shared._White.Gibbing;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeBody()
    {
        SubscribeLocalEvent<BodyComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnBodyMapInit);
    }

    #region Event Handling

    private void OnBeingGibbed(Entity<BodyComponent> body, ref BeingGibbedEvent args)
    {
        foreach (var bodyPart in GetBodyParts(body.AsNullable()))
        {
            foreach (var organ in GetOrgans(bodyPart.AsNullable()))
                args.Giblets.Add(organ);

            PredictedQueueDel(bodyPart.Owner);
        }

        foreach (var item in _inventory.GetHandOrInventoryEntities(body.Owner))
            args.Giblets.Add(item);
    }

    private void OnBodyMapInit(Entity<BodyComponent> body, ref MapInitEvent args)
    {
        var bodyPrototype = Prototype.Index(body.Comp.Prototype);

        if (bodyPrototype.BodyParts.Count != 0)
            SetupBody(body,  bodyPrototype);

        if (bodyPrototype.Organs.Count == 0)
            return;

        body.Comp.Organs = bodyPrototype.Organs.ToDictionary(x => x.Key, x => new OrganSlot(x.Value));
        SetupOrgans(body, body.Comp.Organs);
    }

    #endregion

    #region Private API

    private void SetupBody(Entity<BodyComponent> body, BodyPrototype bodyPrototype)
    {
        var humanoidComponent = CompOrNull<HumanoidAppearanceComponent>(body);
        var coordinates = Comp<TransformComponent>(body).Coordinates;

        var processedBodyPart = new List<string> { bodyPrototype.Root, };
        var queue = new Queue<(string BodyPartId, EntityUid BodyPartParent)>();
        queue.Enqueue((bodyPrototype.Root, body));

        while (queue.TryDequeue(out var id))
        {
            var bodyPartSlot = new BodyPartSlot(bodyPrototype.BodyParts[id.BodyPartId])
            {
                ContainerSlot = _container.EnsureContainer<ContainerSlot>(id.BodyPartParent, GetBodyPartSlotContainerId(id.BodyPartId))
            };

            body.Comp.BodyParts.Add(id.BodyPartId, bodyPartSlot);

            if (TryComp<BodyPartComponent>(id.BodyPartParent, out var parentBodyPartComponent))
            {
                parentBodyPartComponent.BodyParts.Add(id.BodyPartId, bodyPartSlot);
                Dirty(id.BodyPartParent, parentBodyPartComponent);
            }

            if (string.IsNullOrEmpty(bodyPartSlot.StartingBodyPart))
                continue;

            var bodyPart = Spawn(bodyPartSlot.StartingBodyPart, coordinates);
            if (!TryComp<BodyPartComponent>(bodyPart, out var bodyPartComponent))
            {
                _sawmill.Error($"Body part {ToPrettyString(bodyPart)} does not have {typeof(BodyPartComponent)}");
                QueueDel(bodyPart);
                continue;
            }

            if (humanoidComponent != null)
                SetupBodyPartAppearance((body.Owner, humanoidComponent), (bodyPart, bodyPartComponent, null));

            if (!_container.Insert(bodyPart, bodyPartSlot.ContainerSlot))
            {
                _sawmill.Error($"Couldn't insert {ToPrettyString(bodyPart)} to {ToPrettyString(id.BodyPartParent)}");
                QueueDel(bodyPart);
                continue;
            }

            bodyPartComponent.Bones = bodyPartSlot.Bones;
            SetupBones(bodyPart, bodyPartComponent.Bones);

            bodyPartComponent.Organs = bodyPartSlot.Organs;
            SetupOrgans(bodyPart, bodyPartComponent.Organs);

            Dirty(bodyPart, bodyPartComponent);

            foreach (var childBodyPart in bodyPartSlot.Connections)
            {
                if (processedBodyPart.Contains(childBodyPart))
                    continue;

                queue.Enqueue((childBodyPart, bodyPart));
                processedBodyPart.Add(childBodyPart);
            }
        }

        Timer.Spawn(200, () => Dirty(body));
    }

    #endregion
}
