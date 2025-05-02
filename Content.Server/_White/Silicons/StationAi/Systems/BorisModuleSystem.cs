using Content.Shared._White.Silicons.StationAi.Components;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;

namespace Content.Server._White.Silicons.StationAi.Systems;

public sealed class BorisModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorisModuleComponent, MindGotAddedEvent>(OnMindGotAdded);
        SubscribeLocalEvent<BorisModuleComponent, MindRemovedMessage>(OnMindRemoved);

        SubscribeLocalEvent<BorgChassisComponent, AiReturnToCoreEvent>(OnReturnToCore);

        SubscribeLocalEvent<BorisModuleComponent, EntGotRemovedFromContainerMessage>(RemovedFromContainer);
    }

    private void OnMindGotAdded(Entity<BorisModuleComponent> ent, ref MindGotAddedEvent args) => AddReturnAction(ent.Owner, ent.Comp);

    private void OnMindRemoved(Entity<BorisModuleComponent> ent, ref MindRemovedMessage args) => RemoveReturnAction(ent.Owner, ent.Comp);

    public void AddReturnAction(EntityUid uid, BorisModuleComponent component) => _actions.AddAction(uid, ref component.ReturnToCoreActionContainerId, component.ReturnToCoreActionProtoId);

    public void RemoveReturnAction(EntityUid uid, BorisModuleComponent component) => _actions.RemoveAction(uid, component.ReturnToCoreActionContainerId);

    private void OnReturnToCore(Entity<BorgChassisComponent> ent, ref AiReturnToCoreEvent args)
    {
        if (args.Handled || ent.Comp.BrainEntity == null || !TryComp(ent.Comp.BrainEntity, out BorisModuleComponent? moduleComponent))
            return;

        args.Handled = true;
        TryReturnToCore(ent.Owner, moduleComponent);
    }

    private void RemovedFromContainer(Entity<BorisModuleComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID == _stationAi.BorgBrainSlotId)
            TryReturnToCore(ent.Owner, ent.Comp);
    }

    public bool TryReturnToCore(EntityUid uid, BorisModuleComponent component)
    {
        if (component.OriginalBrain == null || Deleted(component.OriginalBrain))
        {
            _popup.PopupEntity("Your original brain no longer exists", uid, uid, PopupType.LargeCaution);
            return false;
        }

        _mind.ControlMob(uid, component.OriginalBrain.Value);
        component.OriginalBrain = null;
        return true;
    }
}
