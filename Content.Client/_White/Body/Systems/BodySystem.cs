using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client._White.Body.Systems;

public sealed partial class BodySystem : SharedBodySystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ComponentHandleState>(OnHandleState);

        InitializeAppearance();
    }

    #region Event Handling

    private void OnHandleState(Entity<BodyComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not BodyComponentState state)
            return;

        foreach (var (key, slot) in ent.Comp.Providers)
        {
            if (state.Providers.ContainsKey(key) || slot.ContainerSlot == null)
                continue;

            _container.ShutdownContainer(slot.ContainerSlot);
            ent.Comp.Providers.Remove(key);
        }

        foreach (var ((key, owner), slot) in state.Providers)
        {
            if (GetEntity(owner) is not { Valid: true } uid)
                continue;

            if (ent.Comp.Providers.TryGetValue((key, owner), out var originSlot))
            {
                originSlot.CopyFrom(slot);
                originSlot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(uid, GetProviderSlotContainerId(key));
                continue;
            }

            originSlot = new (slot)
            {
                ContainerSlot = _container.EnsureContainer<ContainerSlot>(uid, GetProviderSlotContainerId(key)),
            };
            ent.Comp.Providers[(key, owner)] = originSlot;
        }
    }

    #endregion
}
