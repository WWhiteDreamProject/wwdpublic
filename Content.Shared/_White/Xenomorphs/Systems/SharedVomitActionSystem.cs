using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Actions;
using Robust.Shared.Containers;


namespace Content.Shared._White.Xenomorphs.Systems;

public abstract class SharedVomitActionSystem : EntitySystem
{
    /// <inheritdoc/>

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VomitActionComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VomitActionComponent, ComponentShutdown>(OnShutdown);
    }

    protected void OnInit(EntityUid uid, VomitActionComponent component, MapInitEvent args)
    {
        component.Stomach = ContainerSystem.EnsureContainer<Container>(uid, "stomach");

        _actionsSystem.AddAction(uid, ref component.VomitActionEntity, component.VomitAction);
    }

    protected void OnShutdown(EntityUid uid, VomitActionComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.VomitActionEntity);
    }
}

public sealed partial class VomitActionEvent : InstantActionEvent { }
