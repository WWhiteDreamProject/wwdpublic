using Content.Shared._White.Body.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeBodyPart()
    {
        SubscribeLocalEvent<BodyPartComponent, ComponentHandleState>(OnBodyPartHandleState);
    }

    #region Event Handling

    private void OnBodyPartHandleState(Entity<BodyPartComponent> bodyPart, ref ComponentHandleState args)
    {
        if (args.Current is not BodyPartComponentState state)
            return;

        foreach (var (key, slot) in bodyPart.Comp.BodyParts)
        {
            if (state.BodyParts.ContainsKey(key) || slot.ContainerSlot == null)
                continue;

            _container.ShutdownContainer(slot.ContainerSlot);
            bodyPart.Comp.BodyParts.Remove(key);
        }

        foreach (var (key, slot) in bodyPart.Comp.Bones)
        {
            if (state.BodyParts.ContainsKey(key) || slot.ContainerSlot == null)
                continue;

            _container.ShutdownContainer(slot.ContainerSlot);
            bodyPart.Comp.Bones.Remove(key);
        }

        foreach (var (key, slot) in bodyPart.Comp.Organs)
        {
            if (state.Organs.ContainsKey(key) || slot.ContainerSlot == null)
                continue;

            _container.ShutdownContainer(slot.ContainerSlot);
            bodyPart.Comp.Organs.Remove(key);
        }

        foreach (var (stateKey, stateSlot) in state.BodyParts)
        {
            if (bodyPart.Comp.BodyParts.TryGetValue(stateKey, out var slot))
            {
                slot.CopyFrom(stateSlot);
                slot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetBodyPartSlotContainerId(stateKey));
            }
            else
            {
                slot = new (stateSlot)
                {
                    ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetBodyPartSlotContainerId(stateKey))
                };
                bodyPart.Comp.BodyParts[stateKey] = slot;
            }
        }

        foreach (var (stateKey, stateSlot) in state.Bones)
        {
            if (bodyPart.Comp.Bones.TryGetValue(stateKey, out var slot))
            {
                slot.CopyFrom(stateSlot);
                slot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetBoneContainerId(stateKey));
            }
            else
            {
                slot = new (stateSlot)
                {
                    ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetBoneContainerId(stateKey))
                };
                bodyPart.Comp.Bones[stateKey] = slot;
            }
        }

        foreach (var (stateKey, stateSlot) in state.Organs)
        {
            if (bodyPart.Comp.Organs.TryGetValue(stateKey, out var slot))
            {
                slot.CopyFrom(stateSlot);
                slot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetOrganContainerId(stateKey));
            }
            else
            {
                slot = new (stateSlot)
                {
                    ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetOrganContainerId(stateKey))
                };
                bodyPart.Comp.Organs[stateKey] = slot;
            }
        }

        bodyPart.Comp.Type = state.Type;
        bodyPart.Comp.Body = GetEntity(state.Body);
        bodyPart.Comp.Parent = GetEntity(state.Parent);
    }

    #endregion
}
