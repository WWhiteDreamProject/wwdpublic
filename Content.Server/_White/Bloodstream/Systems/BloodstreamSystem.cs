using Content.Server.EntityEffects.Effects;
using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Chemistry.Reagent;
using Content.Shared._White.Particles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Contests;
using Content.Shared.Forensics;
using Content.Shared.HeightAdjust;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server._White.Bloodstream.Systems;

public sealed partial class BloodstreamSystem : SharedBloodstreamSystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly ParticleSystem _particle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private bool _heightAdjust;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BloodstreamComponent, GenerateDnaEvent>(OnDnaGenerated);
        SubscribeLocalEvent<BloodstreamComponent, HeightAdjustedEvent>(OnHeightAdjusted);
        SubscribeLocalEvent<BloodstreamComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);

        InitializeWound();

        Subs.CVar(_configuration, CCVars.HeightAdjustModifiesBloodstream, value => { _heightAdjust = value; }, true);
    }

    #region Event Handling

    private void OnInit(Entity<BloodstreamComponent> ent, ref ComponentInit args)
    {
        if (!SolutionContainer.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out var solution))
            return;

        if (!SolutionContainer.EnsureSolution(ent.Owner, ent.Comp.TemporarySolutionName, out var tempSolution))
            return;

        TryUpdateHeightAdjust(ent);

        solution.MaxVolume = ent.Comp.CurrentMaxVolume + ent.Comp.ChemicalMaxVolume;
        tempSolution.MaxVolume = ent.Comp.BleedPuddleThreshold * 4;

        TryModifyBloodLevel(ent.AsNullable(), ent.Comp.CurrentMaxVolume - solution.Volume);
    }

    private void OnDnaGenerated(Entity<BloodstreamComponent> ent, ref GenerateDnaEvent args)
    {
        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
        {
            Log.Error("Unable to set bloodstream DNA, solution entity could not be resolved");
            return;
        }

        var bloodData = GenerateEntityBloodData(ent.AsNullable());
        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype != ent.Comp.Reagent)
                continue;

            var reagentData = reagent.Reagent.EnsureReagentData();
            reagentData.RemoveAll(x => x is DnaData or BloodReagentData);
            reagentData.AddRange(bloodData);
        }
    }

    private void OnHeightAdjusted(Entity<BloodstreamComponent> ent, ref HeightAdjustedEvent args)
    {
        if (!TryUpdateHeightAdjust(ent))
            return;

        if (!SolutionContainer.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out var solution))
            return;

        solution.MaxVolume = ent.Comp.CurrentMaxVolume + ent.Comp.ChemicalMaxVolume;
        TryModifyBloodLevel(ent.AsNullable(), ent.Comp.Amount * ent.Comp.MassAdjustMultiplier - ent.Comp.Amount);
    }

    private void OnReactionAttempt(Entity<BloodstreamComponent> ent, ref ReactionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var effect in args.Reaction.Effects)
        {
            switch (effect)
            {
                case CreateEntityReactionEffect: // Prevent entities from spawning in the bloodstream
                case AreaReactionEffect: // No spontaneous smoke or foam leaking out of blood vessels.
                    args.Cancelled = true;
                    return;
            }
        }

        // TODO apply organ damage instead of just blocking the reaction?
        // Having cheese-clots form in your veins can't be good for you.
    }

    private void OnReactionAttempt(Entity<BloodstreamComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (args.Name != ent.Comp.SolutionName && args.Name != ent.Comp.TemporarySolutionName)
            return;

        OnReactionAttempt(ent, ref args.Event);
    }

    #endregion

    #region Private API

    private bool TryUpdateHeightAdjust(Entity<BloodstreamComponent> ent)
    {
        if (_heightAdjust || ent.Comp.PowerMassAdjust == 0f)
            return false;

        var factor = Math.Pow(_contests.MassContest(ent, bypassClamp: true, rangeFactor: 4f), ent.Comp.PowerMassAdjust);
        ent.Comp.MassAdjustMultiplier = Math.Clamp(factor, ent.Comp.MinMassAdjust, ent.Comp.MaxMassAdjust);
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.MassAdjustMultiplier));

        return true;
    }

    #endregion
}
