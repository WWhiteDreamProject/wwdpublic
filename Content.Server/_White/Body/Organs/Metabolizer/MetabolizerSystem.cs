using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._White.Body.Bloodstream.Systems;
using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Prototypes;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Body.Organs.Metabolizer;

public sealed class MetabolizerSystem : SharedMetabolizerSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private EntityQuery<BloodstreamComponent> _bloodstreamQuery;
    private EntityQuery<MetabolizerComponent> _metabolizerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();
        _metabolizerQuery = GetEntityQuery<MetabolizerComponent>();

        SubscribeLocalEvent<MetabolizerComponent, AfterOrganToggledEvent>(OnAfterOrganToggled);
        SubscribeLocalEvent<MetabolizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetabolizerComponent, OrganAddedEvent>(OnOrganAdded);
        SubscribeLocalEvent<MetabolizerComponent, OrganHealthChangedEvent>(OnOrganHealthChanged);
        SubscribeLocalEvent<MetabolizerComponent, OrganRelayedEvent<ApplyMetabolicRateEvent>>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<MetabolizerComponent, OrganRemovedEvent>(OnOrganRemoved);
    }

    private void OnAfterOrganToggled(Entity<MetabolizerComponent> ent, ref AfterOrganToggledEvent args) =>
        ent.Comp.Enable = args.Enable;

    private void OnMapInit(Entity<MetabolizerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.AdjustedUpdateInterval;

        foreach (var stageEntry in ent.Comp.Stages.Values)
        {
            if (stageEntry.SolutionOnBody)
                continue;

            stageEntry.SolutionOwner = ent;
            _solutionContainer.ResolveSolution(
                stageEntry.SolutionOwner.Value,
                stageEntry.SolutionName,
                ref stageEntry.Solution,
                out _);
        }
    }

    private void OnOrganAdded(Entity<MetabolizerComponent> ent, ref OrganAddedEvent args)
    {
        if (!_bloodstreamQuery.TryComp(args.Body, out var bloodstream))
            return;

        foreach (var (stage, stageEntry) in ent.Comp.Stages)
        {
            if (stageEntry.SolutionOnBody)
            {
                stageEntry.SolutionOwner = args.Body.Value.Owner;
                _solutionContainer.ResolveSolution(
                    stageEntry.SolutionOwner.Value,
                    stageEntry.SolutionName,
                    ref stageEntry.Solution,
                    out _);
            }

            if (!bloodstream.Stages.TryGetValue(stage, out var stagesEntry))
                continue;

            stagesEntry.Stages.Add(stageEntry);
        }

        ent.Comp.Body = args.Body.Value;
    }

    private void OnOrganHealthChanged(Entity<MetabolizerComponent> ent, ref OrganHealthChangedEvent args) =>
        ent.Comp.UpdateIntervalHealthMultiplier = (args.Organ.Comp2.MaximumHealth / args.Organ.Comp2.Health).Float();

    private void OnApplyMetabolicMultiplier(Entity<MetabolizerComponent> ent, ref OrganRelayedEvent<ApplyMetabolicRateEvent> args) =>
        ent.Comp.UpdateIntervalMultiplier = args.Args.Rate == 0 ? 0 : 1 / args.Args.Rate;

    private void OnOrganRemoved(Entity<MetabolizerComponent> ent, ref OrganRemovedEvent args)
    {
        if (!_bloodstreamQuery.TryComp(args.Body, out var bloodstream))
            return;

        foreach (var (stage, stageEntry) in ent.Comp.Stages)
        {
            if (stageEntry.SolutionOnBody)
            {
                stageEntry.SolutionOwner = null;
                stageEntry.Solution = null;
            }

            if (!bloodstream.Stages.TryGetValue(stage, out var stagesEntry))
                continue;

            stagesEntry.Stages.Remove(stageEntry);
        }

        ent.Comp.Body = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MetabolizerComponent>();
        while (query.MoveNext(out var uid, out var metabolizer))
        {
            if (metabolizer.AdjustedUpdateInterval == TimeSpan.Zero || _gameTiming.CurTime < metabolizer.NextUpdate)
                continue;

            metabolizer.NextUpdate += metabolizer.AdjustedUpdateInterval;
            Metabolize((uid, metabolizer));
        }
    }

    private bool TryGetTransferSolution(
        Entity<BloodstreamComponent?> ent,
        ProtoId<MetabolismStagePrototype> stage,
        [NotNullWhen(true)] out Solution? transferSolution
    )
    {
        transferSolution = null;
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Stages.TryGetValue(stage, out var currentStages) || !currentStages.NextStage.HasValue)
            return false;

        if (!ent.Comp.Stages.TryGetValue(currentStages.NextStage.Value, out var nextStages))
            return false;

        var pickedNextStage = _random.Pick(nextStages.Stages);
        if (pickedNextStage.SolutionOwner is null)
            return false;

        return _solutionContainer.ResolveSolution(
            pickedNextStage.SolutionOwner.Value,
            pickedNextStage.SolutionName,
            ref pickedNextStage.Solution,
            out transferSolution);
    }

    public void Metabolize(Entity<MetabolizerComponent?> ent)
    {
        if (!_metabolizerQuery.Resolve(ent, ref ent.Comp) || !ent.Comp.Enable)
            return;

        var owner = ent.Owner;
        if (ent.Comp.Body.HasValue)
            owner = ent.Comp.Body.Value;

        var isDead = _mobState.IsDead(owner);

        foreach (var (stage, stageEntry) in ent.Comp.Stages)
        {
            if (stageEntry.SolutionOwner is not {} solutionOwner
                || !_solutionContainer.ResolveSolution(
                    solutionOwner,
                    stageEntry.SolutionName,
                    ref stageEntry.Solution,
                    out var solution))
                continue;

            TryGetTransferSolution(owner, stage, out var transferSolution);

            // Copy the solution do not edit the original solution list
            var reagents = solution.Contents.ToList();
            if (_bloodstreamQuery.TryComp(solutionOwner, out var bloodstream))
                reagents = _bloodstream.GetChemicals((solutionOwner, bloodstream), solution);

            // randomize the reagent list so we don't have any unique quirks
            _random.Shuffle(reagents);

            var processedReagents = 0;
            foreach (var (reagent, quantity) in reagents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                    continue;

                var mostToTransfer = FixedPoint2.Clamp(stageEntry.TransferRate, 0, quantity);
                if (proto.Metabolisms is null || !proto.Metabolisms.TryGetValue(stage, out var entry))
                {
                    if (transferSolution is not null)
                    {
                        solution.RemoveReagent(reagent, mostToTransfer);
                        transferSolution.AddReagent(reagent, mostToTransfer * stageEntry.TransferEfficacy);
                    }
                    else
                        solution.RemoveReagent(reagent, FixedPoint2.New(1));

                    continue;
                }

                var rate = stageEntry.MetabolizeAll ? quantity : entry.MetabolismRate;
                var mostToRemove = FixedPoint2.Clamp(rate, 0, quantity);

                if (processedReagents >= ent.Comp.MaxReagentsProcessable)
                    return;

                var scale = mostToRemove.Float();
                if (!stageEntry.MetabolizeAll)
                    scale /= rate.Float();

                if (isDead && !proto.WorksOnTheDead)
                    continue;

                var ev = new TryMetabolizeReagent(reagent, proto, quantity);
                RaiseLocalEvent(owner, ref ev);

                var args = new EntityEffectReagentArgs(owner, EntityManager, ent, solution, mostToRemove, proto, null, scale);

                foreach (var effect in entry.Effects)
                {
                    if (!effect.ShouldApply(args, _random))
                        continue;

                    effect.Effect(args);

                    if (!effect.ShouldLog)
                        continue;

                    _adminLog.Add(
                        LogType.ReagentEffect,
                        effect.LogImpact,
                        $"Metabolism effect {effect.GetType().Name:effect}"
                        + $" of reagent {proto.LocalizedName:reagent}"
                        + $" applied on entity {owner:entity}"
                        + $" at {Transform(owner).Coordinates:coordinates}"
                    );
                }

                if (mostToRemove == FixedPoint2.Zero)
                    continue;

                solution.RemoveReagent(reagent, mostToRemove);
                processedReagents++;

                if (transferSolution is null)
                    continue;

                foreach (var (metabolite, ratio) in entry.Metabolites)
                    transferSolution.AddReagent(metabolite, mostToRemove * ratio);
            }

            _solutionContainer.UpdateChemicals(stageEntry.Solution.Value);
        }
    }

    public void AddMetabolizerTypes(Entity<MetabolizerComponent?> ent, HashSet<ProtoId<MetabolizerTypePrototype>> metabolizerTypes)
    {
        if (!_metabolizerQuery.Resolve(ent, ref ent.Comp))
            return;

        foreach (var metabolizerType in metabolizerTypes)
            ent.Comp.Types.Add(metabolizerType);
    }
}

[ByRefEvent]
public record struct TryMetabolizeReagent(ReagentId Reagent, ReagentPrototype Prototype, FixedPoint2 Quantity, float Scale = 1f, float QuantityMultiplier = 1f);
