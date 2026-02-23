using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared._White.Chemistry.Reagent;
using Content.Shared._White.Gibbing;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Components;
using Content.Shared.HealthExaminable;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._White.Body.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem : EntitySystem
{
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;

    private EntityQuery<BloodstreamComponent> _bloodstreamQuery;

    public override void Initialize()
    {
        base.Initialize();

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();

        SubscribeLocalEvent<BloodstreamComponent, ApplyMetabolicRateEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BloodstreamComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BloodstreamComponent, GibbedBeforeDeletionEvent>(OnGibbedBeforeDeletion);
        SubscribeLocalEvent<BloodstreamComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodstreamComponent, RejuvenateEvent>(OnRejuvenate);

        InitializeBodyPart();
        InitializeGenerator();
        InitializeWound();
    }

    #region Event Handling

    private void OnApplyMetabolicMultiplier(Entity<BloodstreamComponent> ent, ref ApplyMetabolicRateEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Rate == 0 ? 0 : 1 / args.Rate;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.UpdateIntervalMultiplier));
    }

    private void OnEntRemoved(Entity<BloodstreamComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution and set it to null
        if (args.Entity == entity.Comp.BloodSolution?.Owner)
            entity.Comp.BloodSolution = null;

        if (args.Entity == entity.Comp.TemporarySolution?.Owner)
            entity.Comp.TemporarySolution = null;
    }

    private void OnGibbedBeforeDeletion(Entity<BloodstreamComponent> ent, ref GibbedBeforeDeletionEvent args) =>
        SpillAllSolutions(ent.AsNullable());

    private void OnHealthBeingExamined(Entity<BloodstreamComponent> ent, ref HealthBeingExaminedEvent args)
    {
        // Shows profusely bleeding at half the max bleed rate.
        if (ent.Comp.Bleeding > ent.Comp.MaximumBleeding / 2)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(
                !args.IsSelfAware
                    ? Loc.GetString("bloodstream-component-profusely-bleeding", ("target", ent.Owner))
                    : Loc.GetString("bloodstream-component-selfaware-profusely-bleeding"));
        }
        // Shows bleeding message when bleeding, but less than profusely.
        else if (ent.Comp.Bleeding > 0)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(
                !args.IsSelfAware
                    ? Loc.GetString("bloodstream-component-bleeding", ("target", ent.Owner))
                    : Loc.GetString("bloodstream-component-selfaware-bleeding"));
        }

        // If the mob's blood level is below the damage threshhold, the pale message is added.
        if (GetBloodLevel(ent.AsNullable()) < 0.9f)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(
                !args.IsSelfAware
                    ? Loc.GetString("bloodstream-component-looks-pale", ("target", ent.Owner))
                    : Loc.GetString("bloodstream-component-selfaware-looks-pale"));
        }
    }

    private void OnMapInit(Entity<BloodstreamComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.AdjustedUpdateInterval;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.NextUpdate));
    }

    private void OnRejuvenate(Entity<BloodstreamComponent> ent, ref RejuvenateEvent args)
    {
        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out _))
            return;

        SolutionContainer.RemoveAllSolution(ent.Comp.BloodSolution.Value);
        TryModifyBloodLevel(ent.AsNullable(), ent.Comp.CurrentBloodMaxVolume - GetBloodAmount(ent.AsNullable()));

        UpdateBloodstream(ent);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (bloodstream.AdjustedUpdateInterval == TimeSpan.Zero || curTime < bloodstream.NextUpdate)
                continue;

            UpdateBloodstream((uid, bloodstream));
        }
    }

    #region Private API

    private bool IsCompatibleBlood(BloodstreamComponent bloodstream, ReagentId reagent) =>
        reagent.Prototype == bloodstream.BloodReagent
        && reagent.EnsureReagentData()
            .Any(x => x is BloodReagentData bloodReagentData
                && BloodGroupCompatible(bloodReagentData.BloodGroup, bloodstream.BloodGroup));

    /// <summary>
    /// Gets new blood data for this entity and caches it in <see cref="BloodstreamComponent.BloodData"/>
    /// </summary>
    protected List<ReagentData> GenerateEntityBloodData(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return new List<ReagentData>();

        var reagentData = new List<ReagentData>
        {
            new BloodReagentData
            {
                BloodGroup = ent.Comp.BloodGroup
            },
            new DnaData
            {
                DNA = TryComp<DnaComponent>(ent, out var dnaComponent)
                    ? dnaComponent.DNA
                    : Loc.GetString("forensics-dna-unknown")
            }
        };

        ent.Comp.BloodData = reagentData;
        return reagentData;
    }

    private void UpdateBloodstream(Entity<BloodstreamComponent> ent)
    {
        ent.Comp.NextUpdate += ent.Comp.AdjustedUpdateInterval;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.NextUpdate));

        var bloodAmount = GetBloodAmount(ent.AsNullable());
        // Adds blood to their blood level if it is below the maximum; Blood regeneration. Must be alive.
        if (bloodAmount < ent.Comp.CurrentBloodMaxVolume && !_mobState.IsDead(ent))
        {
            var getReductionEvent = new GetBloodReductionEvent(FixedPoint2.Zero);
            RaiseLocalEvent(ent, ref getReductionEvent);

            if (getReductionEvent.BloodReduction > FixedPoint2.Zero)
                TryModifyBloodLevel(ent.AsNullable(), getReductionEvent.BloodReduction);
        }

        if (bloodAmount > ent.Comp.CurrentBloodMaxVolume)
            TryModifyBloodLevel(ent.AsNullable(), ent.Comp.CurrentBloodMaxVolume - bloodAmount);

        var getBleedEvent = new GetBleedEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref getBleedEvent);

        var oldBleeding = ent.Comp.Bleeding;
        ent.Comp.Bleeding = FixedPoint2.Clamp(getBleedEvent.Bleeding, 0, ent.Comp.MaximumBleeding);
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Bleeding));

        TickBleed(ent);

        if (oldBleeding == ent.Comp.Bleeding)
            return;

        RaiseLocalEvent(ent, new AfterBleedingChangedEvent(ent.Comp.Bleeding, oldBleeding));

        if (ent.Comp.Bleeding == 0)
            _alerts.ClearAlert(ent, ent.Comp.BleedingAlert);
        else
        {
            var severity = (short)Math.Clamp(Math.Round(ent.Comp.Bleeding.Float(), MidpointRounding.ToZero), 0, 10);
            _alerts.ShowAlert(ent, ent.Comp.BleedingAlert, severity);
        }
    }

    private void TickBleed(Entity<BloodstreamComponent> ent)
    {
        if (ent.Comp.Bleeding <= 0)
            return;

        var ev = new BleedModifierEvent(ent.Comp.Bleeding, 0);
        RaiseLocalEvent(ent, ref ev);

        // Blood is removed from the bloodstream at a 1-1 rate with the bleed amount
        TryBleedOut(ent.AsNullable(), ev.Bleeding);

        // Bleed rate is reduced by the bleed reduction amount in the bloodstream component.
        TryModifyBleedAmount(ent.AsNullable(), -ev.BleedReductionAmount);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempt to transfer a provided solution to internal solution.
    /// </summary>
    public bool TryAddToBloodstream(Entity<BloodstreamComponent?> ent, Solution solution)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution))
            return false;

        var bloodAmount = GetBloodAmount(ent);

        if (!SolutionContainer.TryAddSolution(ent.Comp.BloodSolution.Value, solution))
            return false;

        var newBloodAmount = FixedPoint2.Zero;
        foreach (var reagent in solution.Contents)
        {
            if (!IsCompatibleBlood(ent.Comp, reagent.Reagent))
                continue;

            newBloodAmount += reagent.Quantity;
        }

        if (newBloodAmount == FixedPoint2.Zero)
            return true;

        RaiseLocalEvent(ent, new AfterBloodAmountChangedEvent((ent, ent.Comp), bloodAmount + newBloodAmount, bloodAmount));
        return true;
    }

    /// <summary>
    /// Removes blood by spilling out the bloodstream.
    /// </summary>
    public bool TryBleedOut(Entity<BloodstreamComponent?> ent, FixedPoint2 amount)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution)
            || amount <= 0)
            return false;

        var leakedBlood = SolutionContainer.SplitSolution(ent.Comp.BloodSolution.Value, amount);

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodTemporarySolutionName, ref ent.Comp.TemporarySolution, out var tempSolution))
            return true;

        tempSolution.AddSolution(leakedBlood, _prototype);

        if (tempSolution.Volume > ent.Comp.BleedPuddleThreshold)
        {
            _puddle.TrySpillAt(ent.Owner, tempSolution, out _, sound: false);

            tempSolution.RemoveAllSolution();
        }

        SolutionContainer.UpdateChemicals(ent.Comp.TemporarySolution.Value);

        return true;
    }

    /// <summary>
    /// Tries to make an entity bleed more or less.
    /// </summary>
    public bool TryModifyBleedAmount(Entity<BloodstreamComponent?> ent, FixedPoint2 amount, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        // TODO: Move it into BloodstreamSystem.Wound
        var woundAmount = amount;
        var wounds = _wound.GetWounds<BleedingWoundComponent>(ent, scar: true, bodyPartType: bodyPartType);
        foreach (var wound in wounds)
        {
            if (woundAmount == 0f)
                break;

            woundAmount -= TryModifyWoundBleedAmount((wound, wound.Comp2), woundAmount);
        }

        ent.Comp.Bleeding = FixedPoint2.Clamp(ent.Comp.Bleeding + (amount - woundAmount), 0, ent.Comp.MaximumBleeding);
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Bleeding));

        if (ent.Comp.Bleeding == 0)
            _alerts.ClearAlert(ent.Owner, ent.Comp.BleedingAlert);
        else
        {
            var severity = (short)Math.Clamp(Math.Round(ent.Comp.Bleeding.Float(), MidpointRounding.ToZero), 0, 10);
            _alerts.ShowAlert(ent.Owner, ent.Comp.BleedingAlert, severity);
        }

        return true;
    }

    /// <summary>
    /// Attempts to modify the blood level of this entity directly.
    /// </summary>
    public bool TryModifyBloodLevel(Entity<BloodstreamComponent?> ent, FixedPoint2 amount)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out _)
            || amount == 0)
            return false;

        var bloodAmount = GetBloodAmount(ent);
        var bloodData = GetEntityBloodData(ent);
        var acceptedQuantity = FixedPoint2.Zero;

        amount = FixedPoint2.Min(amount, ent.Comp.CurrentBloodMaxVolume - bloodAmount);

        if (amount > 0 && !SolutionContainer.TryAddReagent(ent.Comp.BloodSolution.Value, ent.Comp.BloodReagent, amount, out acceptedQuantity, data: bloodData))
            return false;

        if (amount < 0)
            acceptedQuantity = -SolutionContainer.RemoveReagent(ent.Comp.BloodSolution.Value, ent.Comp.BloodReagent, amount, bloodData);

        if (acceptedQuantity == FixedPoint2.Zero)
            return false;

        RaiseLocalEvent(ent, new AfterBloodAmountChangedEvent((ent, ent.Comp), bloodAmount + acceptedQuantity, bloodAmount));

        return true;
    }

    /// <summary>
    /// Attempts to take blood from the entity.
    /// </summary>
    public bool TryTakeBlood(Entity<BloodstreamComponent?> ent, FixedPoint2 quantity, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution))
            return false;

        solution = SolutionContainer.SplitSolution(ent.Comp.BloodSolution.Value, quantity);
        return solution.Volume > 0;
    }

    /// <summary>
    /// Returns the current blood amount.
    /// </summary>
    public FixedPoint2 GetBloodAmount(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return FixedPoint2.Zero;

        var bloodAmount = FixedPoint2.Zero;
        foreach (var reagent in bloodSolution.Contents)
        {
            if (!IsCompatibleBlood(ent.Comp, reagent.Reagent))
                continue;

            bloodAmount += reagent.Quantity;
        }

        return bloodAmount;
    }

    /// <summary>
    /// Returns the current blood level as a percentage (between 0 and 1).
    /// </summary>
    public float GetBloodLevel(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return 0.0f;

        return GetBloodAmount(ent).Float() / ent.Comp.CurrentBloodMaxVolume.Float();
    }

    /// <summary>
    /// Get the reagent data for blood that a specific entity should have.
    /// </summary>
    public List<ReagentData> GetEntityBloodData(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return new List<ReagentData>();

        return ent.Comp.BloodData ?? GenerateEntityBloodData(ent);
    }

    /// <summary>
    /// Returns chemicals in the bloodstream.
    /// </summary>
    public List<ReagentQuantity> GetChemicals(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return new List<ReagentQuantity>();

        return GetChemicals(ent, bloodSolution);
    }

    /// <summary>
    /// Returns chemicals in the bloodstream.
    /// </summary>
    public List<ReagentQuantity> GetChemicals(Entity<BloodstreamComponent?> ent, Solution solution)
    {
        var reagents =  new List<ReagentQuantity>();
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return reagents;

        foreach (var reagent in solution.Contents)
        {
            if (IsCompatibleBlood(ent.Comp, reagent.Reagent))
                continue;

            reagents.Add(reagent);
        }

        return reagents;
    }

    /// <summary>
    /// Removes a certain number of all reagents except of a single excluded one from the bloodstream and blood itself.
    /// </summary>
    /// <returns>
    /// Solution of removed chemicals or null if none were removed.
    /// </returns>
    public Solution? FlushChemicals(Entity<BloodstreamComponent?> ent, FixedPoint2 quantity, ProtoId<ReagentPrototype>? excludedReagentId = null)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return null;

        var flushedSolution = new Solution();
        foreach (var reagent in bloodSolution.Contents)
        {
            if (reagent.Reagent.Prototype == excludedReagentId
                || reagent.Reagent.Prototype == ent.Comp.BloodReagent)
                continue;

            var reagentFlushAmount = SolutionContainer.RemoveReagent(ent.Comp.BloodSolution.Value, reagent.Reagent, quantity);
            flushedSolution.AddReagent(reagent.Reagent, reagentFlushAmount);
        }

        return flushedSolution.Volume == 0 ? null : flushedSolution;
    }

    /// <summary>
    /// Change what someone's blood is made of, on the fly.
    /// </summary>
    public void ChangeBloodReagent(Entity<BloodstreamComponent?> ent, ProtoId<ReagentPrototype> reagent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false) || reagent == ent.Comp.BloodReagent)
            return;

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
        {
            ent.Comp.BloodReagent = reagent;
            return;
        }

        var currentVolume = bloodSolution.RemoveReagent(ent.Comp.BloodReagent, bloodSolution.Volume, ignoreReagentData: true);

        ent.Comp.BloodReagent = reagent;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodReagent));

        if (currentVolume > 0)
            SolutionContainer.TryAddReagent(ent.Comp.BloodSolution.Value, ent.Comp.BloodReagent, currentVolume, null, GetEntityBloodData(ent));
    }

    /// <summary>
    /// Spill all bloodstream solutions into a puddle.
    /// BLOOD FOR THE BLOOD GOD
    /// </summary>
    public void SpillAllSolutions(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return;

        var tempSol = new Solution();

        if (SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
        {
            tempSol.MaxVolume += bloodSolution.MaxVolume;
            tempSol.AddSolution(bloodSolution, _prototype);
            SolutionContainer.RemoveAllSolution(ent.Comp.BloodSolution.Value);
        }

        if (SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodTemporarySolutionName, ref ent.Comp.TemporarySolution, out var tempSolution))
        {
            tempSol.MaxVolume += tempSolution.MaxVolume;
            tempSol.AddSolution(tempSolution, _prototype);
            SolutionContainer.RemoveAllSolution(ent.Comp.TemporarySolution.Value);
        }

        _puddle.TrySpillAt(ent, tempSol, out _);
    }

    /// <summary>
    /// Checks whether the donor's blood group matches the recipient's.
    /// </summary>
    /// <param name="donor">Donor's blood group.</param>
    /// <param name="recipient">Recipient's blood group.</param>
    public static bool BloodGroupCompatible(BloodGroup donor, BloodGroup recipient) =>
        BloodTypeCompatible(donor.Type, recipient.Type)
        && BloodRhesusFactorCompatible(donor.RhesusFactor, recipient.RhesusFactor);

    /// <summary>
    /// Checks whether the donor's blood type matches the recipient's.
    /// </summary>
    /// <param name="donor">Donor's blood type.</param>
    /// <param name="recipient">Recipient's blood type</param>
    public static bool BloodTypeCompatible(BloodType donor, BloodType recipient) =>
        recipient switch
        {
            BloodType.O => donor is BloodType.O,
            BloodType.A => donor is BloodType.A or BloodType.O,
            BloodType.B => donor is BloodType.B or BloodType.O,
            BloodType.AB => true,
            _ => false
        };

    /// <summary>
    /// Checks whether the donor's blood Rhesus factor matches the recipient's.
    /// </summary>
    /// <param name="donor">Donor's blood Rhesus factor.</param>
    /// <param name="recipient">Recipient's blood Rhesus factor</param>
    public static bool BloodRhesusFactorCompatible(BloodRhesusFactor donor, BloodRhesusFactor recipient) =>
        donor == BloodRhesusFactor.Negative || recipient == BloodRhesusFactor.Positive;

    #endregion
}

/// <summary>
/// Raised on an entity after its blood level has changed.
/// </summary>
public record struct AfterBloodAmountChangedEvent(Entity<BloodstreamComponent> Bloodstream, FixedPoint2 BloodAmount, FixedPoint2 OldBloodAmount);

/// <summary>
/// Raised on an entity after its bleeding has changed.
/// </summary>
public record struct AfterBleedingChangedEvent(FixedPoint2 Bleeding, FixedPoint2 OldBleeding);

/// <summary>
/// Raised on an entity before they bleed to modify the amount.
/// </summary>
/// <param name="Bleeding">The amount of blood the entity will lose.</param>
/// <param name="BleedReductionAmount">The amount of bleed reduction that will happen.</param>
[ByRefEvent]
public record struct BleedModifierEvent(FixedPoint2 Bleeding, float BleedReductionAmount);

/// <summary>
/// Raised on an entity to get the sum total of bleeding.
/// </summary>
[ByRefEvent]
public record struct GetBleedEvent(FixedPoint2 Bleeding);

/// <summary>
/// Raised on an entity to get the sum total of blood reduction.
/// </summary>
[ByRefEvent]
public record struct GetBloodReductionEvent(FixedPoint2 BloodReduction);
