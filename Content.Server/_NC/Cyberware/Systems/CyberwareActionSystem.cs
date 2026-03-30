using Content.Shared._NC.Cyberware.Components;
using Content.Shared.Actions;
using Robust.Shared.Containers;

namespace Content.Server._NC.Cyberware.Systems;

/// <summary>
///     Система, отвечающая за выдачу и удаление экшенов при установке/извлечении киберимплантов.
/// </summary>
public sealed class CyberwareActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareActionComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<CyberwareActionComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(EntityUid uid, CyberwareActionComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CyberwareComponent.ContainerName)
            return;

        if (string.IsNullOrEmpty(component.ActionId))
            return;

        _actions.AddAction(args.Container.Owner, ref component.ActionEntity, component.ActionId, uid);
    }

    private void OnRemoved(EntityUid uid, CyberwareActionComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CyberwareComponent.ContainerName)
            return;

        if (component.ActionEntity != null)
        {
            _actions.RemoveProvidedActions(args.Container.Owner, uid);
        }
    }
}
