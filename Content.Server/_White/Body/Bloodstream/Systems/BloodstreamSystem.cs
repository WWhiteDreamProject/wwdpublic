using Content.Server.EntityEffects.Effects;
using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Bloodstream.Systems;
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

namespace Content.Server._White.Body.Bloodstream.Systems;

public sealed partial class BloodstreamSystem : SharedBloodstreamSystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly ParticleSystem _particle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, GenerateDnaEvent>(OnDnaGenerated);
        SubscribeLocalEvent<BloodstreamComponent, HeightAdjustedEvent>(OnHeightAdjusted);
        SubscribeLocalEvent<BloodstreamComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);

        InitializeWound();
    }

    #region Event Handling
    private void OnComponentInit(Entity<BloodstreamComponent> ent, ref ComponentInit args)
    {
        if (!SolutionContainer.EnsureSolution(ent.Owner, ent.Comp.BloodSolutionName, out var bloodSolution)
            || !SolutionContainer.EnsureSolution(ent.Owner, ent.Comp.BloodTemporarySolutionName, out var tempSolution))
            return;

        if (ent.Comp.PowerMassAdjust != 0f && _configuration.GetCVar(CCVars.HeightAdjustModifiesBloodstream))
        {
            var factor = Math.Pow(_contests.MassContest(ent, bypassClamp: true, rangeFactor: 4f), ent.Comp.PowerMassAdjust);
            ent.Comp.BloodMaxVolumeMultiplier = Math.Clamp(factor, ent.Comp.MinMassAdjust, ent.Comp.MaxMassAdjust);
            DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodMaxVolumeMultiplier));
        }

        bloodSolution.MaxVolume = ent.Comp.CurrentBloodMaxVolume + ent.Comp.ChemicalMaxVolume;
        tempSolution.MaxVolume = ent.Comp.BleedPuddleThreshold * 4; // give some leeway, for chem stream as well

        // Fill a blood solution with BLOOD
        bloodSolution.AddReagent(new ReagentId(ent.Comp.BloodReagent, GetEntityBloodData(ent.AsNullable())), ent.Comp.CurrentBloodMaxVolume - bloodSolution.Volume);
    }

    private void OnDnaGenerated(Entity<BloodstreamComponent> ent, ref GenerateDnaEvent args)
    {
        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
        {
            Log.Error("Unable to set bloodstream DNA, solution entity could not be resolved");
            return;
        }

        var bloodData = GenerateEntityBloodData(ent.AsNullable());
        foreach (var reagent in bloodSolution.Contents)
        {
            if (reagent.Reagent.Prototype != ent.Comp.BloodReagent)
                continue;

            var reagentData = reagent.Reagent.EnsureReagentData();
            reagentData.RemoveAll(x => x is DnaData or BloodReagentData);
            reagentData.AddRange(bloodData);
        }
    }

    private void OnHeightAdjusted(Entity<BloodstreamComponent> ent, ref HeightAdjustedEvent args)
    {
        if (ent.Comp.PowerMassAdjust == 0f
            || !_configuration.GetCVar(CCVars.HeightAdjustModifiesBloodstream)
            || !SolutionContainer.EnsureSolution(ent.Owner, ent.Comp.BloodSolutionName, out var bloodSolution))
            return;

        var factor = Math.Pow(_contests.MassContest(ent, bypassClamp: true, rangeFactor: 4f), ent.Comp.PowerMassAdjust);
        factor = Math.Clamp(factor, ent.Comp.MinMassAdjust, ent.Comp.MaxMassAdjust);
        ent.Comp.BloodMaxVolumeMultiplier = factor;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodMaxVolumeMultiplier));

        var bloodAmount = GetBloodAmount(ent.AsNullable());
        bloodSolution.MaxVolume = ent.Comp.CurrentBloodMaxVolume + ent.Comp.ChemicalMaxVolume;
        TryModifyBloodLevel(ent.AsNullable(), bloodAmount * factor - bloodAmount);
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
        if (args.Name != ent.Comp.BloodSolutionName && args.Name != ent.Comp.BloodTemporarySolutionName)
            return;

        OnReactionAttempt(ent, ref args.Event);
    }

    #endregion
}
