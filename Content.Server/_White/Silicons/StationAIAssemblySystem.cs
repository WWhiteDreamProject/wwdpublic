using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared._White.Silicons.StationAi;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Network;


namespace Content.Server._White.Silicons;


/// <summary>
/// This handles...
/// </summary>
public sealed class StationAIAssemblySystem : EntitySystem
{
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StationJobsSystem _jobs = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCoreComponent, StationAIDisassembleDoAfterEvent>(OnDisassembleDoAfter);
    }

    private void OnDisassembleDoAfter(Entity<StationAiCoreComponent> ent, ref StationAIDisassembleDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var assembly = Spawn(ent.Comp.AssemblyProto, Transform(ent.Owner).Coordinates);
        var assemblyComp = EnsureComp<StationAIAssemblyComponent>(assembly);
        var aiBrainsInContainer = _container.GetContainer(ent.Owner, StationAiCoreComponent.Container).ContainedEntities;

        var cover = Spawn(assemblyComp.CoverMaterialPrototype, Transform(assembly).Coordinates);
        _stack.SetCount(cover, assemblyComp.CoverMaterialStackSize);

        if(Prototype(ent.Owner)!.ID == "PlayerStationAi")
            _jobs.TryAdjustJobSlot(_station.GetStationInMap(Transform(_roundEnd.GetStation()!.Value).MapID)!.Value, "StationAi", -1);

        if (aiBrainsInContainer.Count == 0)
        {
            QueueDel(ent.Owner);
            return;
        }

        var aiBrain = aiBrainsInContainer[0];
        var assemblyBrain = SpawnInContainerOrDrop(ent.Comp.InsertedBrain, assembly, assemblyComp.BrainSlotId);

        if (_mind.TryGetMind(aiBrain, out var mindId, out var mind))
            _mind.TransferTo(mindId, assemblyBrain, mind: mind);

        QueueDel(ent.Owner);
    }
}
