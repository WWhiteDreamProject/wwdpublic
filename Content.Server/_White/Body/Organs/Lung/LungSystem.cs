using Content.Server._White.Body.Organs.Metabolizer;
using Content.Server._White.Body.Respirator.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.EntityEffects.EffectConditions;
using Content.Server.EntityEffects.Effects;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Prototypes;
using Content.Shared._White.Body.Systems;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Body.Organs.Lung;

public sealed class LungSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private static readonly ProtoId<MetabolismStagePrototype> RespirationId = new("Respiration");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LungComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<LungComponent, OrganRelayedEvent<BeforeBreathEvent>>(OnBeforeBreath);
        SubscribeLocalEvent<LungComponent, OrganRelayedEvent<CanMetabolizeGasEvent>>(OnCanMetabolizeGas);
        SubscribeLocalEvent<LungComponent, OrganRelayedEvent<ExhaledGasEvent>>(OnExhaledGas);
        SubscribeLocalEvent<LungComponent, OrganRelayedEvent<InhaledGasEvent>>(OnInhaledGas);
        SubscribeLocalEvent<LungComponent, OrganRelayedEvent<StopSuffocatingEvent>>(OnStopSuffocating);
        SubscribeLocalEvent<LungComponent, OrganRelayedEvent<SuffocationEvent>>(OnSuffocation);
    }

    private void OnComponentInit(Entity<LungComponent> ent, ref ComponentInit args)
    {
        if (!_solutionContainer.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out var solution))
            return;

        solution.MaxVolume = ent.Comp.MaxVolume;
        solution.CanReact = ent.Comp.CanReact;
    }

    private void OnBeforeBreath(Entity<LungComponent> ent, ref OrganRelayedEvent<BeforeBreathEvent> args) =>
        args.Args = new (args.Args.BreathVolume + ent.Comp.AdjustedBreathVolume);

    private void OnCanMetabolizeGas(Entity<LungComponent> ent, ref OrganRelayedEvent<CanMetabolizeGasEvent> args)
    {
        var solution = GasToReagent(args.Args.Gas);

        var saturation = GetSaturation(solution, ent.Owner, out var toxic);
        if (toxic)
        {
            args.Args = args.Args with { Toxic = true, };
            return;
        }

        args.Args = args.Args with { Saturation = saturation, };
    }

    private void OnExhaledGas(Entity<LungComponent> ent, ref OrganRelayedEvent<ExhaledGasEvent> args)
    {
        var outGas = new GasMixture(args.Args.Gas.Volume);

        _atmosphere.Merge(outGas, ent.Comp.Air);
        ent.Comp.Air.Clear();

        if (_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution))
            _solutionContainer.RemoveAllSolution(ent.Comp.Solution.Value);

        _atmosphere.Merge(args.Args.Gas, outGas);
    }

    private void OnInhaledGas(Entity<LungComponent> ent, ref OrganRelayedEvent<InhaledGasEvent> args) =>
        args.Args = args.Args with { Succeeded = TryInhaleGas(ent.AsNullable(), args.Args.Gas), };

    private void OnStopSuffocating(Entity<LungComponent> ent, ref OrganRelayedEvent<StopSuffocatingEvent> args) =>
        _alerts.ClearAlert(args.Body, ent.Comp.Alert);

    private void OnSuffocation(Entity<LungComponent> ent, ref OrganRelayedEvent<SuffocationEvent> args) =>
        _alerts.ShowAlert(args.Body, ent.Comp.Alert);

    private bool TryInhaleGas(Entity<LungComponent?> ent, GasMixture gas)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.AdjustedBreathVolume == 0)
            return false;

        _atmosphere.Merge(ent.Comp.Air, gas);
        GasToReagent(ent);
        _metabolizer.Metabolize(ent.Owner);

        return true;
    }

    /// <summary>
    /// Get the amount of saturation that would be generated if the lung were to metabolize the given solution.
    /// </summary>
    /// <remarks>
    /// This assumes the metabolism rate is unbounded, which generally should be the case for lungs, otherwise we get
    /// back to the old pulmonary edema bug.
    /// </remarks>
    /// <param name="solution">The reagents to metabolize</param>
    /// <param name="lung">The entity doing the metabolizing</param>
    /// <param name="toxic">Whether or not any of the reagents would deal damage to the entity</param>
    private float GetSaturation(Solution solution, Entity<MetabolizerComponent?> lung, out bool toxic)
    {
        toxic = false;
        if (!Resolve(lung, ref lung.Comp))
            return 0;

        float saturation = 0;
        foreach (var (id, quantity) in solution.Contents)
        {
            var reagent = _prototype.Index<ReagentPrototype>(id.Prototype);
            if (reagent.Metabolisms == null || !reagent.Metabolisms.TryGetValue(RespirationId, out var entry))
                continue;

            foreach (var effect in entry.Effects)
            {
                if (effect is HealthChange health)
                    toxic |= CanMetabolize(health) && health.Damage.AnyPositive();
                else if (effect is Oxygenate oxy && CanMetabolize(oxy))
                    saturation += oxy.Factor * quantity.Float();
            }
        }

        // this is pretty janky, but I just want to bodge a method that checks if an entity can breathe a gas mixture
        // Applying actual reaction effects require a full ReagentEffectArgs struct.
        bool CanMetabolize(EntityEffect effect)
        {
            if (effect.Conditions == null)
                return true;

            // TODO: Use Metabolism Public API to do this instead, once that API has been built.
            foreach (var cond in effect.Conditions)
            {
                if (cond is MetabolizerType metabolizerType && !metabolizerType.Condition(lung, EntityManager))
                    return false;
            }

            return true;
        }

        return saturation;
    }

    public void GasToReagent(Entity<LungComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp)
            || !_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        GasToReagent(ent.Comp.Air, solution);
        _solutionContainer.UpdateChemicals(ent.Comp.Solution.Value);
    }

    public Solution GasToReagent(GasMixture gas)
    {
        var solution = new Solution();
        GasToReagent(gas, solution);
        return solution;
    }

    private void GasToReagent(GasMixture gas, Solution solution)
    {
        foreach (var gasId in Enum.GetValues<Gas>())
        {
            var i = (int) gasId;
            var moles = gas[i];
            if (moles <= 0)
                continue;

            var reagent = _atmosphere.GasReagents[i];
            if (reagent is null)
                continue;

            var amount = moles * Atmospherics.BreathMolesToReagentMultiplier;
            solution.AddReagent(reagent, amount);
        }
    }
}
