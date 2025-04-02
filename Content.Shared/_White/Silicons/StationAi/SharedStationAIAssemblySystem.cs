using System.Buffers;
using System.Runtime.Serialization;
using Content.Shared.Cabinet;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Mind;
using Content.Shared.Prying.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;


namespace Content.Shared._White.Silicons.StationAi;


public sealed class SharedStationAIAssemblySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAIAssemblyComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<StationAIAssemblyComponent, EntRemovedFromContainerMessage>(OnContainerModified);

        SubscribeLocalEvent<StationAIAssemblyComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<StationAIAssemblyComponent, StationAIAssemblyDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<StationAiCoreComponent, InteractUsingEvent>(OnDisassemble);
    }

    private void OnContainerModified(EntityUid uid, StationAIAssemblyComponent component, ContainerModifiedMessage args)
    {
        _appearance.SetData(uid, StationAIAssemblyVisuals.HasBrain, _itemSlots.GetItemOrNull(uid,component.BrainSlotId) != null);
    }

    private void OnInteract(Entity<StationAIAssemblyComponent> ent, ref InteractUsingEvent args)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.BrainSlotId) == null
            || !TryComp(args.Used, out StackComponent? stackComponent)
            || stackComponent.StackTypeId != ent.Comp.CoverMaterialStackPrototype
            || _stack.GetCount(args.Used) < ent.Comp.CoverMaterialStackSize)
            return;

        var ev = new StationAIAssemblyDoAfterEvent
        {
            InteractedWith = GetNetEntity(args.Used)
        };
        _doAfter.TryStartDoAfter(
            new(EntityManager, args.User, TimeSpan.FromSeconds(1), ev, ent.Owner)
        {
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true
        });
    }

    private void OnDoAfter(Entity<StationAIAssemblyComponent> ent, ref StationAIAssemblyDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var brain = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.BrainSlotId);
        if (!_net.IsServer || !_mind.TryGetMind(brain!.Value, out var mindId, out var mind))
            return;
        
        _stack.SetCount(GetEntity(args.InteractedWith), _stack.GetCount(GetEntity(args.InteractedWith)) - ent.Comp.CoverMaterialStackSize);

        var ai = SpawnAtPosition(ent.Comp.StationAIPrototype, Transform(ent.Owner).Coordinates);
        var aiBrain = SpawnInContainerOrDrop(_stationAi.DefaultAi, ai, StationAiCoreComponent.Container);
        _mind.TransferTo(mindId, aiBrain, mind: mind);

        var aiComp = EnsureComp<StationAiCoreComponent>(ai);
        aiComp.InsertedBrain = Prototype(brain.Value)!.ID;

        QueueDel(ent.Owner);
    }

    private void OnDisassemble(Entity<StationAiCoreComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp(ent.Owner, out WiresPanelComponent? wiresComponent)
            && !wiresComponent.Open
            || !HasComp<PryingComponent>(args.Used))
            return;

        var ev = new StationAIDisassembleDoAfterEvent();
        _doAfter.TryStartDoAfter(
            new(EntityManager, args.User, TimeSpan.FromSeconds(5), ev, ent.Owner)
            {
                BreakOnMove = true,
                NeedHand = true,
                BlockDuplicate = true
            });
    }


}

[Serializable, NetSerializable]
public sealed partial class StationAIAssemblyDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity InteractedWith;
}

[Serializable, NetSerializable]
public sealed partial class StationAIDisassembleDoAfterEvent : SimpleDoAfterEvent { }
