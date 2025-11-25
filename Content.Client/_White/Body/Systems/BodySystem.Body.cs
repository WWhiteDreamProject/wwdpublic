using Content.Shared._White.Body.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeBody() => SubscribeLocalEvent<BodyComponent, ComponentHandleState>(OnBodyHandleState);

    #region Event Handling

    private void OnBodyHandleState(Entity<BodyComponent> body, ref ComponentHandleState args)
    {
        if (args.Current is not BodyComponentState state)
            return;

        foreach (var (key, slot) in body.Comp.BodyParts)
        {
            if (state.BodyParts.ContainsKey(key) || slot.ContainerSlot == null)
                continue;

            _container.ShutdownContainer(slot.ContainerSlot);
            body.Comp.BodyParts.Remove(key);
        }

        foreach (var (key, slot) in body.Comp.Organs)
        {
            if (state.Organs.ContainsKey(key) || slot.ContainerSlot == null)
                continue;

            _container.ShutdownContainer(slot.ContainerSlot);
            body.Comp.Organs.Remove(key);
        }

        foreach (var (stateKey, (stateSlot, stateOwner)) in state.BodyParts)
        {
            if (GetEntity(stateOwner) is not {} uid || !uid.Valid)
                continue;

            if (body.Comp.BodyParts.TryGetValue(stateKey, out var slot))
            {
                slot.CopyFrom(stateSlot);
                slot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(uid, GetBodyPartSlotContainerId(stateKey));
            }
            else
            {
                slot = new (stateSlot)
                {
                    ContainerSlot = _container.EnsureContainer<ContainerSlot>(uid, GetBodyPartSlotContainerId(stateKey))
                };
                body.Comp.BodyParts[stateKey] = slot;
            }
        }

        foreach (var (stateKey, (stateSlot, stateOwner)) in state.Organs)
        {
            if (GetEntity(stateOwner) is not {} uid || !uid.Valid)
                continue;

            if (body.Comp.Organs.TryGetValue(stateKey, out var slot))
            {
                slot.CopyFrom(stateSlot);
                slot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(uid, GetOrganContainerId(stateKey));
            }
            else
            {
                slot = new (stateSlot)
                {
                    ContainerSlot = _container.EnsureContainer<ContainerSlot>(uid, GetOrganContainerId(stateKey))
                };
                body.Comp.Organs[stateKey] = slot;
            }
        }
    }

    #endregion
}
