using System.Linq;
using Content.Server._White.Respirator.Components;
using Content.Server.EntityEffects.Effects;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;

namespace Content.Server._White.Respirator.Systems;

public sealed partial class RespiratorSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<RespiratorProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<RespiratorProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<RespiratorProviderComponent, BodyRelayedEvent<CanMetabolizeGasEvent>>(OnCanMetabolizeGas);
        SubscribeLocalEvent<RespiratorProviderComponent, BodyRelayedEvent<ExhaleEvent>>(OnExhale);
        SubscribeLocalEvent<RespiratorProviderComponent, BodyRelayedEvent<GetBreathVolumeEvent>>(OnGetBreathVolume);
        SubscribeLocalEvent<RespiratorProviderComponent, BodyRelayedEvent<InhaleEvent>>(OnInhale);
        SubscribeLocalEvent<RespiratorProviderComponent, BodyRelayedEvent<SuffocationChangedEvent>>(OnSuffocationChanged);
        SubscribeLocalEvent<RespiratorProviderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RespiratorProviderComponent, WoundableSeverityChangedEvent>(OnWoundableSeverityChanged);
    }

    #region Event Handling

    private void OnGotInserted(Entity<RespiratorProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        ent.Comp.Body = args.Body;
        UpdateVolume(args.Body.Owner);
    }

    private void OnGotRemoved(Entity<RespiratorProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        UpdateVolume(args.Body.Owner);
    }

    // TODO: This shit from Wizden, need refactor in future.
    private void OnCanMetabolizeGas(Entity<RespiratorProviderComponent> ent, ref BodyRelayedEvent<CanMetabolizeGasEvent> args)
    {
        if (ent.Comp.Body is not {} body)
            return;

        var solution = GasToReagent(args.Args.Gas);

        var toxic = false;
        var saturation = 0f;

        foreach (var (id, quantity) in solution.Contents)
        {
            var reagent = _prototype.Index<ReagentPrototype>(id.Prototype);
            if (reagent.Metabolisms == null || !reagent.Metabolisms.TryGetValue(RespirationId, out var entry))
                continue;

            var effectArgs = new EntityEffectReagentArgs(body, EntityManager, ent, solution, quantity, reagent, null, 1f);
            foreach (var effect in entry.Effects)
            {
                if (effect.Conditions != null && effect.Conditions.Any(x => !x.Condition(effectArgs)))
                    continue;

                if (effect is HealthChange healthChange)
                    toxic |= healthChange.Damage.AnyPositive();

                if (effect is Oxygenate oxygenate)
                    saturation += oxygenate.Factor * quantity.Float();
            }
        }

        if (toxic)
        {
            args.Args = args.Args with { Toxic = true, };
            return;
        }

        args.Args = args.Args with { Saturation = saturation, };
    }

    private void OnExhale(Entity<RespiratorProviderComponent> ent, ref BodyRelayedEvent<ExhaleEvent> args)
    {
        var outGas = new GasMixture(args.Args.Gas.Volume);

        _atmosphere.Merge(outGas, ent.Comp.Air);
        ent.Comp.Air.Clear();

        if (_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution))
            _solutionContainer.RemoveAllSolution(ent.Comp.Solution.Value);

        _atmosphere.Merge(args.Args.Gas, outGas);
    }

    private void OnGetBreathVolume(Entity<RespiratorProviderComponent> ent, ref BodyRelayedEvent<GetBreathVolumeEvent> args)
    {
        args.Args = new (args.Args.Volume + ent.Comp.Volume);
    }

    private void OnInhale(Entity<RespiratorProviderComponent> ent, ref BodyRelayedEvent<InhaleEvent> args)
    {
        if (ent.Comp.Volume == 0)
            return;

        _atmosphere.Merge(ent.Comp.Air, args.Args.Gas);
        GasToReagent(ent);
        _metabolizer.Metabolize(ent.Owner);

        args.Args = args.Args with { Succeeded = true, };
    }

    private void OnSuffocationChanged(Entity<RespiratorProviderComponent> ent, ref BodyRelayedEvent<SuffocationChangedEvent> args)
    {
        if (ent.Comp.Body is not {} body)
            return;

        if (args.Args.Suffocation == 0)
        {
            _alerts.ClearAlert(body, ent.Comp.Alert);
            return;
        }

        _alerts.ShowAlert(body, ent.Comp.Alert);
    }

    private void OnInit(Entity<RespiratorProviderComponent> ent, ref ComponentInit args)
    {
        if (!_solutionContainer.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out var solution))
            return;

        solution.MaxVolume = ent.Comp.SolutionMaxVolume;
        solution.CanReact = ent.Comp.SolutionCanReact;
    }

    private void OnWoundableSeverityChanged(Entity<RespiratorProviderComponent> ent, ref WoundableSeverityChangedEvent args)
    {
        if (!ent.Comp.VolumeThresholds.TryGetValue(args.Severity, out var volume))
            return;

        ent.Comp.Volume = volume;
    }

    #endregion

    #region Public API

    public Solution GasToReagent(GasMixture gas)
    {
        var solution = new Solution();
        GasToReagent(gas, solution);
        return solution;
    }

    #endregion

    #region Private API

    private void GasToReagent(Entity<RespiratorProviderComponent> ent)
    {
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        GasToReagent(ent.Comp.Air, solution);
        _solutionContainer.UpdateChemicals(ent.Comp.Solution.Value);
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

    #endregion
}
