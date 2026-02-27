using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body.Prototypes;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Wounds.Systems;
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

namespace Content.Server._White.Bloodstream.Systems;

public sealed class MetabolizerSystem : SharedMetabolizerSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private EntityQuery<MetabolizerComponent> _metabolizerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolizerComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<MetabolizerComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<MetabolizerComponent, BodyRelayedEvent<MetabolicRateChangedEvent>>(OnMetabolicRateChanged);
        SubscribeLocalEvent<MetabolizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetabolizerComponent, WoundableSeverityChangedEvent>(OnWoundableSeverityChanged);

        _metabolizerQuery = GetEntityQuery<MetabolizerComponent>();
    }

    #region Event Handling

    private void OnGotInserted(Entity<MetabolizerComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        if (!BloodstreamQuery.TryComp(args.Body, out var bloodstreamComp))
            return;

        foreach (var (stage, stageEntry) in ent.Comp.Stages)
        {
            if (stageEntry.SolutionOnBody)
            {
                stageEntry.SolutionOwner = args.Body;
                _solutionContainer.ResolveSolution(stageEntry.SolutionOwner.Value, stageEntry.SolutionName, ref stageEntry.Solution, out _);
            }

            if (!bloodstreamComp.Stages.TryGetValue(stage, out var stagesEntry))
                continue;

            stagesEntry.Stages.Add(stageEntry);
        }

        ent.Comp.Body = args.Body;
    }

    private void OnGotRemoved(Entity<MetabolizerComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (!BloodstreamQuery.TryComp(args.Body, out var bloodstreamComp))
            return;

        foreach (var (stage, stageEntry) in ent.Comp.Stages)
        {
            if (stageEntry.SolutionOnBody)
            {
                stageEntry.SolutionOwner = null;
                stageEntry.Solution = null;
            }

            if (!bloodstreamComp.Stages.TryGetValue(stage, out var stagesEntry))
                continue;

            stagesEntry.Stages.Remove(stageEntry);
        }

        ent.Comp.Body = null;
    }

    private void OnMetabolicRateChanged(Entity<MetabolizerComponent> ent, ref BodyRelayedEvent<MetabolicRateChangedEvent> args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Args.Rate == 0 ? 0 : 1 / args.Args.Rate;
    }

    private void OnMapInit(Entity<MetabolizerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.CurrentUpdateInterval;

        foreach (var stageEntry in ent.Comp.Stages.Values)
        {
            if (stageEntry.SolutionOnBody)
                continue;

            stageEntry.SolutionOwner = ent;
            _solutionContainer.ResolveSolution(stageEntry.SolutionOwner.Value, stageEntry.SolutionName, ref stageEntry.Solution, out _);
        }
    }

    private void OnWoundableSeverityChanged(Entity<MetabolizerComponent> ent, ref WoundableSeverityChangedEvent args)
    {
        if (!ent.Comp.UpdateIntervalThresholds.TryGetValue(args.Severity, out var updateInterval))
            return;

        ent.Comp.UpdateInterval = updateInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MetabolizerComponent>();
        while (query.MoveNext(out var uid, out var metabolizer))
        {
            if (metabolizer.CurrentUpdateInterval == TimeSpan.Zero || _gameTiming.CurTime < metabolizer.NextUpdate)
                continue;

            metabolizer.NextUpdate += metabolizer.CurrentUpdateInterval;
            Metabolize((uid, metabolizer));
        }
    }

    #endregion

    #region Public API

    public void AddMetabolizerTypes(Entity<MetabolizerComponent?> ent, HashSet<ProtoId<MetabolizerTypePrototype>> types)
    {
        if (!_metabolizerQuery.Resolve(ent, ref ent.Comp))
            return;

        foreach (var type in types)
            ent.Comp.Types.Add(type);
    }

    public void Metabolize(Entity<MetabolizerComponent?> ent)
    {
        if (!_metabolizerQuery.Resolve(ent, ref ent.Comp))
            return;

        var owner = ent.Owner;
        if (ent.Comp.Body.HasValue)
            owner = ent.Comp.Body.Value;

        var isDead = _mobState.IsDead(owner);

        foreach (var (stage, stageEntry) in ent.Comp.Stages)
        {
            if (stageEntry.SolutionOwner is not {} solutionOwner)
                continue;

            if (!_solutionContainer.ResolveSolution(solutionOwner, stageEntry.SolutionName, ref stageEntry.Solution, out var solution))
                continue;

            TryGetTransferSolution(owner, stage, out var transferSolution);

            // Copy the solution do not edit the original solution list
            var reagents = solution.Contents.ToList();
            if (BloodstreamQuery.TryComp(solutionOwner, out var bloodstreamComp))
                reagents = _bloodstream.GetChemicals((solutionOwner, bloodstreamComp), solution);

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

    #endregion

    #region Private API

    private void TryGetTransferSolution(
        Entity<BloodstreamComponent?> ent,
        ProtoId<MetabolismStagePrototype> stage,
        [NotNullWhen(true)] out Solution? transferSolution
    )
    {
        transferSolution = null;
        if (!BloodstreamQuery.Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Stages.TryGetValue(stage, out var currentStages) || !currentStages.NextStage.HasValue)
            return;

        if (!ent.Comp.Stages.TryGetValue(currentStages.NextStage.Value, out var nextStages))
            return;

        var nextStage = _random.Pick(nextStages.Stages);
        if (nextStage.SolutionOwner is null)
            return;

        _solutionContainer.ResolveSolution(nextStage.SolutionOwner.Value, nextStage.SolutionName, ref nextStage.Solution, out transferSolution);
    }

    #endregion
}

[ByRefEvent]
public record struct TryMetabolizeReagent(ReagentId Reagent, ReagentPrototype Prototype, FixedPoint2 Quantity, float Scale = 1f, float QuantityMultiplier = 1f);
