using Content.Shared._White.Body.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeBone()
    {
        SubscribeLocalEvent<BoneComponent, ComponentHandleState>(OnBoneHandleState);
    }

    #region Event Handling

    private void OnBoneHandleState(Entity<BoneComponent> bone, ref ComponentHandleState args)
    {
        if (args.Current is not BoneComponentState state)
            return;

        foreach (var (key, slot) in bone.Comp.Organs)
        {
            if (state.Organs.ContainsKey(key) || slot.ContainerSlot == null)
                continue;

            _container.ShutdownContainer(slot.ContainerSlot);
            bone.Comp.Organs.Remove(key);
        }

        foreach (var (stateKey, stateSlot) in state.Organs)
        {
            if (bone.Comp.Organs.TryGetValue(stateKey, out var slot))
            {
                slot.CopyFrom(stateSlot);
                slot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(bone, GetOrganContainerId(stateKey));
            }
            else
            {
                slot = new (stateSlot)
                {
                    ContainerSlot = _container.EnsureContainer<ContainerSlot>(bone, GetOrganContainerId(stateKey))
                };
                bone.Comp.Organs[stateKey] = slot;
            }
        }

        bone.Comp.Type = state.Type;
        bone.Comp.Body = GetEntity(state.Body);
        bone.Comp.Parent = GetEntity(state.Parent);
    }

    #endregion
}
