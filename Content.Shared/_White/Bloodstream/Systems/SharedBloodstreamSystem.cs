using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Chemistry.Reagent;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Gibbable.Systems;
using Content.Shared._White.Helpers;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._White.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem : EntitySystem
{
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;

    private EntityQuery<BloodstreamComponent> _bloodstreamQuery;
    private EntityQuery<BloodstreamProviderComponent> _providerQuery;
    private EntityQuery<BleedingWoundComponent> _woundQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BloodstreamComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BloodstreamComponent, GibbedBeforeDeletionEvent>(OnGibbedBeforeDeletion);
        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodstreamComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BloodstreamComponent, SolutionContainerChangedEvent>(OnSolutionContainerChanged);

        InitializeAccumulator();
        InitializeProvider();
        InitializeWound();

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();
        _providerQuery = GetEntityQuery<BloodstreamProviderComponent>();
        _woundQuery = GetEntityQuery<BleedingWoundComponent>();
    }

    #region Event Handling

    private void OnShutdown(Entity<BloodstreamComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent, ent.Comp.AlertCategory);
    }

    private void OnEntRemoved(Entity<BloodstreamComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution and set it to null
        if (args.Entity == entity.Comp.Solution?.Owner)
            entity.Comp.Solution = null;

        if (args.Entity != entity.Comp.TemporarySolution?.Owner)
            return;

        entity.Comp.TemporarySolution = null;
    }

    private void OnGibbedBeforeDeletion(Entity<BloodstreamComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        SpillAllSolutions(ent.AsNullable());
    }

    protected virtual void OnMapInit(Entity<BloodstreamComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.CurrentUpdateInterval;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.NextUpdate));
    }

    private void OnRejuvenate(Entity<BloodstreamComponent> ent, ref RejuvenateEvent args)
    {
        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out _))
            return;

        SolutionContainer.RemoveAllSolution(ent.Comp.Solution.Value);
        SyncBloodAmount(ent);

        TryModifyBloodLevel(ent.AsNullable(), ent.Comp.CurrentMaxVolume - ent.Comp.Amount);

        UpdateBleeding(ent.Owner);
        UpdateBloodReduction(ent.Owner);
    }

    private void OnSolutionContainerChanged(Entity<BloodstreamComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.SolutionName)
            return;

        SyncBloodAmount(ent, args.Solution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (bloodstream.NextBleedingUpdate is { } nextBleedingUpdate && curTime >= nextBleedingUpdate)
                UpdateBleeding((uid, bloodstream));

            if (bloodstream.CurrentUpdateInterval == TimeSpan.Zero || curTime < bloodstream.NextUpdate)
                continue;

            UpdateBloodstream((uid, bloodstream));
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempt to transfer a provided solution to internal solution.
    /// </summary>
    public bool TryAddToBloodstream(Entity<BloodstreamComponent?> ent, Solution solution)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution))
            return false;

        if (!SolutionContainer.TryAddSolution(ent.Comp.Solution.Value, solution))
            return false;

        SyncBloodAmount((ent, ent.Comp));
        return true;
    }

    /// <summary>
    /// Removes blood by spilling out the bloodstream.
    /// </summary>
    public bool TryBleedOut(Entity<BloodstreamComponent?> ent, FixedPoint2 amount)
    {
        if (amount <= 0)
            return true;

        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution))
            return false;

        var leakedBlood = SolutionContainer.SplitSolution(ent.Comp.Solution.Value, amount);
        SyncBloodAmount((ent, ent.Comp));

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.TemporarySolutionName, ref ent.Comp.TemporarySolution, out var tempSolution))
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
    /// Tries to change the total bleeding of a body.
    /// </summary>
    /// <remarks>
    /// A body without bleeding wounds can't be made to bleed this way.
    /// </remarks>
    /// <param name="ent">The body whose bleeding should be changed.</param>
    /// <param name="amount">How much to change the total bleeding by. Negative to stop bleeding.</param>
    /// <returns>True if the bleeding has changed.</returns>
    public bool TryModifyBleeding(Entity<BloodstreamComponent?> ent, FixedPoint2 amount)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, false) || amount == FixedPoint2.Zero)
            return false;

        RefreshBleeding((ent, ent.Comp));

        var bleeding = ent.Comp.Bleeding;
        if (bleeding <= FixedPoint2.Zero)
            return false;

        var ev = new ModifyBleedingEvent(amount);
        RaiseLocalEvent(ent, ref ev);

        UpdateBleeding(ent);
        return ent.Comp.Bleeding != bleeding;
    }

    /// <summary>
    /// Attempts to modify the blood level of this entity directly.
    /// </summary>
    public bool TryModifyBloodLevel(Entity<BloodstreamComponent?> ent, FixedPoint2 amount)
    {
        if (amount == 0)
            return true;

        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out _))
            return false;

        var bloodData = GetEntityBloodData(ent);

        if (amount > 0)
        {
            amount = FixedPoint2.Min(amount, FixedPoint2.Max(FixedPoint2.Zero, ent.Comp.CurrentMaxVolume - ent.Comp.Amount));
            if (amount == FixedPoint2.Zero)
                return true;

            if (!SolutionContainer.TryAddReagent(ent.Comp.Solution.Value, ent.Comp.Reagent, amount, out _, data: bloodData))
                return false;
        }
        else
        {
            SolutionContainer.RemoveReagent(ent.Comp.Solution.Value, ent.Comp.Reagent, -amount, bloodData);
        }

        SyncBloodAmount((ent, ent.Comp));

        return true;
    }

    /// <summary>
    /// Attempts to take blood from the entity.
    /// </summary>
    public bool TryTakeBlood(Entity<BloodstreamComponent?> ent, FixedPoint2 quantity, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution))
            return false;

        solution = SolutionContainer.SplitSolution(ent.Comp.Solution.Value, quantity);
        SyncBloodAmount((ent, ent.Comp));

        return solution.Volume > 0;
    }

    /// <summary>
    /// Returns the current blood amount.
    /// </summary>
    public FixedPoint2 GetBloodAmount(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return FixedPoint2.Zero;

        return ent.Comp.Amount;
    }

    /// <summary>
    /// Returns the current blood level as a percentage (between 0 and 1).
    /// </summary>
    public float GetBloodLevel(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return 0.0f;

        return ent.Comp.Level;
    }

    /// <summary>
    /// Returns the current metabolic rate.
    /// </summary>
    public float GetMetabolicRate(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return 0.0f;

        return ent.Comp.MetabolicRate;
    }

    /// <summary>
    /// Get the reagent data for blood that a specific entity should have.
    /// </summary>
    public List<ReagentData> GetEntityBloodData(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return new List<ReagentData>();

        return ent.Comp.Data ?? GenerateEntityBloodData(ent);
    }

    /// <summary>
    /// Returns chemicals in the bloodstream.
    /// </summary>
    public List<ReagentQuantity> GetChemicals(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return new List<ReagentQuantity>();

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var bloodSolution))
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
            if (BloodCompatible(ent.Comp, reagent.Reagent))
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
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false))
            return null;

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var bloodSolution))
            return null;

        var flushedSolution = new Solution();
        foreach (var reagent in bloodSolution.Contents)
        {
            if (reagent.Reagent.Prototype == excludedReagentId || reagent.Reagent.Prototype == ent.Comp.Reagent)
                continue;

            var reagentFlushAmount = SolutionContainer.RemoveReagent(ent.Comp.Solution.Value, reagent.Reagent, quantity);
            flushedSolution.AddReagent(reagent.Reagent, reagentFlushAmount);
        }

        return flushedSolution.Volume == 0 ? null : flushedSolution;
    }

    /// <summary>
    /// Change what someone's blood is made of, on the fly.
    /// </summary>
    public void ChangeBloodReagent(Entity<BloodstreamComponent?> ent, ProtoId<ReagentPrototype> reagent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, logMissing: false) || reagent == ent.Comp.Reagent)
            return;

        var oldReagent = ent.Comp.Reagent;

        ent.Comp.Reagent = reagent;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Reagent));

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var bloodSolution))
            return;

        var currentVolume = bloodSolution.RemoveReagent(oldReagent, bloodSolution.Volume, ignoreReagentData: true);
        if (currentVolume > 0)
            SolutionContainer.TryAddReagent(ent.Comp.Solution.Value, ent.Comp.Reagent, currentVolume, null, GetEntityBloodData(ent));

        SyncBloodAmount((ent, ent.Comp));
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

        if (SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var bloodSolution))
        {
            tempSol.MaxVolume += bloodSolution.MaxVolume;
            tempSol.AddSolution(bloodSolution, _prototype);
            SolutionContainer.RemoveAllSolution(ent.Comp.Solution.Value);
        }

        if (SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.TemporarySolutionName, ref ent.Comp.TemporarySolution, out var tempSolution))
        {
            tempSol.MaxVolume += tempSolution.MaxVolume;
            tempSol.AddSolution(tempSolution, _prototype);
            SolutionContainer.RemoveAllSolution(ent.Comp.TemporarySolution.Value);
        }

        _puddle.TrySpillAt(ent, tempSol, out _);

        SyncBloodAmount((ent, ent.Comp));
    }

    /// <summary>
    /// Gathers the bleeding from all sources (body parts and their wounds) and caches it.
    /// Called automatically when the sources change; between those calls the bleeding is extrapolated from the cache.
    /// </summary>
    public void UpdateBleeding(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new GetBleedingEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.BleedingBase = ev.Bleeding;
        ent.Comp.BleedingDecay = ev.Decay;
        ent.Comp.BleedingUpdatedAt = _gameTiming.CurTime;
        ent.Comp.NextBleedingUpdate = ev.NextChange;

        RefreshBleeding((ent, ent.Comp));
    }

    /// <summary>
    /// Gathers the blood regeneration from all sources and caches it.
    /// </summary>
    public void UpdateBloodReduction(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new GetBloodReductionEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref ev);

        if (ent.Comp.BloodReduction == ev.Reduction)
            return;

        ent.Comp.BloodReduction = ev.Reduction;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodReduction));
    }

    /// <summary>
    /// Checks whether the donor's blood group matches the recipient's.
    /// </summary>
    /// <param name="donor">Donor's blood group.</param>
    /// <param name="recipient">Recipient's blood group.</param>
    public static bool BloodGroupCompatible(BloodGroup donor, BloodGroup recipient)
    {
        return BloodTypeCompatible(donor.Type, recipient.Type) && BloodRhesusFactorCompatible(donor.RhesusFactor, recipient.RhesusFactor);
    }

    /// <summary>
    /// Checks whether the donor's blood type matches the recipient's.
    /// </summary>
    /// <param name="donor">Donor's blood type.</param>
    /// <param name="recipient">Recipient's blood type</param>
    public static bool BloodTypeCompatible(BloodType donor, BloodType recipient)
    {
        return recipient switch
        {
            BloodType.O => donor is BloodType.O,
            BloodType.A => donor is BloodType.A or BloodType.O,
            BloodType.B => donor is BloodType.B or BloodType.O,
            BloodType.AB => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks whether the donor's blood Rhesus factor matches the recipient's.
    /// </summary>
    /// <param name="donor">Donor's blood Rhesus factor.</param>
    /// <param name="recipient">Recipient's blood Rhesus factor</param>
    public static bool BloodRhesusFactorCompatible(BloodRhesusFactor donor, BloodRhesusFactor recipient)
    {
        return donor == BloodRhesusFactor.Negative || recipient == BloodRhesusFactor.Positive;
    }

    #endregion

    #region Private API

    /// <summary>
    /// Checks if a given reagent is compatible with the entity's blood type.
    /// </summary>
    private bool BloodCompatible(BloodstreamComponent component, ReagentId reagent)
    {
        if (component.Reagent != reagent.Prototype)
            return false;

        foreach (var data in reagent.EnsureReagentData())
        {
            if (data is not BloodReagentData bloodData)
                continue;

            if (!BloodGroupCompatible(bloodData.Group, component.Group))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Updates the current bleeding from the cached sources: the cached bleeding minus its decay over the elapsed time.
    /// </summary>
    private void RefreshBleeding(Entity<BloodstreamComponent> ent)
    {
        var elapsed = (float) (_gameTiming.CurTime - ent.Comp.BleedingUpdatedAt).TotalSeconds;
        var bleeding = ent.Comp.BleedingBase - FixedPoint2.New(ent.Comp.BleedingDecay * MathF.Max(elapsed, 0f));

        SetBodyBleeding(ent, FixedPoint2.Max(bleeding, FixedPoint2.Zero));
    }

    private void SetBodyBleeding(Entity<BloodstreamComponent> ent, FixedPoint2 bleeding)
    {
        var bleedingDelta = bleeding - ent.Comp.Bleeding;
        if (bleedingDelta == 0)
            return;

        ent.Comp.Bleeding += bleedingDelta;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Bleeding));

        var ev = new BleedingChangedEvent(bleedingDelta);
        RaiseLocalEvent(ent, ref ev);

        UpdateAlert(ent);

        var bleedingLevel = ent.Comp.BleedingThresholds.HighestMatch(ent.Comp.Bleeding) ?? BleedingLevel.Zero;
        if (ent.Comp.BleedingLevel == bleedingLevel)
            return;

        ent.Comp.BleedingLevel = bleedingLevel;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BleedingLevel));

        var levelEv = new BleedingLevelChangedEvent(bleedingLevel);
        RaiseLocalEvent(ent, ref levelEv);
    }

    /// <summary>
    /// Recalculates the cached blood amount from the actual contents of the blood solution.
    /// </summary>
    private void SyncBloodAmount(Entity<BloodstreamComponent> ent)
    {
        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        SyncBloodAmount(ent, solution);
    }

    /// <inheritdoc cref="SyncBloodAmount(Entity{BloodstreamComponent})"/>
    private void SyncBloodAmount(Entity<BloodstreamComponent> ent, Solution solution)
    {
        var amount = FixedPoint2.Zero;
        foreach (var reagent in solution.Contents)
        {
            if (!BloodCompatible(ent.Comp, reagent.Reagent))
                continue;

            amount += reagent.Quantity;
        }

        var delta = amount - ent.Comp.Amount;
        if (delta == FixedPoint2.Zero)
            return;

        ent.Comp.Amount = amount;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Amount));

        ent.Comp.Level = (ent.Comp.Amount / ent.Comp.CurrentMaxVolume).Float();
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Level));

        var ev = new BloodAmountChangedEvent(delta, ent.Comp.Level);
        RaiseLocalEvent(ent, ref ev);
    }

    private void UpdateAlert(Entity<BloodstreamComponent> ent)
    {
        if (ent.Comp.Bleeding == 0)
        {
            _alerts.ClearAlertCategory(ent, ent.Comp.AlertCategory);
            return;
        }

        if (!ent.Comp.Alerts.TryGetValue(ent.Comp.BleedingLevel, out var alert))
        {
            Log.Error($"No alert for bleeding level {ent.Comp.BleedingLevel} for entity {ToPrettyString(ent)}");
            return;
        }

        var severity = _alerts.GetMinSeverity(alert);
        if (ent.Comp.BleedingThresholds.TryGetNextValue(ent.Comp.BleedingLevel, out var nextLevel)
            && ent.Comp.BleedingThresholds.TryGetKey(nextLevel, out var threshold))
        {
            var blend = FixedPoint2.Clamp(ent.Comp.Bleeding / threshold, 0, 1).Float();

            severity = (short) MathF.Round(MathHelper.Lerp(severity, _alerts.GetMaxSeverity(alert), blend));
        }

        _alerts.ShowAlert(ent, alert, severity);
    }

    private void UpdateBleedOut(Entity<BloodstreamComponent> ent)
    {
        if (ent.Comp.Bleeding == 0)
            return;

        var ev = new BleedModifierEvent(ent.Comp.Bleeding, 0);
        RaiseLocalEvent(ent, ref ev);

        TryBleedOut(ent.AsNullable(), ev.Bleeding);

        if (ev.Reduction <= FixedPoint2.Zero)
            return;

        TryModifyBleeding(ent.AsNullable(), -ev.Reduction);
    }

    private void UpdateBloodstream(Entity<BloodstreamComponent> ent)
    {
        ent.Comp.NextUpdate += ent.Comp.CurrentUpdateInterval;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.NextUpdate));

        if (ent.Comp.BloodReduction > FixedPoint2.Zero && ent.Comp.Amount < ent.Comp.CurrentMaxVolume)
            TryModifyBloodLevel(ent.AsNullable(), ent.Comp.BloodReduction);

        RefreshBleeding(ent);

        UpdateBleedOut(ent);
    }

    /// <summary>
    /// Gets new blood data for this entity and caches it in <see cref="BloodstreamComponent.Data"/>
    /// </summary>
    protected List<ReagentData> GenerateEntityBloodData(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp))
            return new List<ReagentData>();

        var reagentData = new List<ReagentData>
        {
            new BloodReagentData
            {
                Group = ent.Comp.Group
            },
            new DnaData
            {
                DNA = TryComp<DnaComponent>(ent, out var dnaComponent)
                    ? dnaComponent.DNA
                    : Loc.GetString("forensics-dna-unknown")
            }
        };

        ent.Comp.Data = reagentData;
        return reagentData;
    }

    #endregion
}

/// <summary>
/// Event raised on an entity after its bleeding has changed.
/// </summary>
/// <param name="Bleeding">The amount by which the bleeding has changed.</param>
[ByRefEvent]
public record struct BleedingChangedEvent(FixedPoint2 Bleeding);

/// <summary>
/// Event raised on an entity after its bleeding level has been changed.
/// </summary>
/// <param name="Level">The new bleeding level.</param>
/// <param name="Location">The specific body location when bleeding level changed.</param>
[ByRefEvent]
public record struct BleedingLevelChangedEvent(BleedingLevel Level, BodyProviderType Location = BodyProviderType.All);

/// <summary>
/// Event raised on an entity after its blood level has changed.
/// </summary>
/// <param name="Amount">The amount by which the blood has changed.</param>
/// <param name="Level">The current blood level.</param>
[ByRefEvent]
public record struct BloodAmountChangedEvent(FixedPoint2 Amount, float Level) : IBodyRelayEvent
{
    BodyProviderType IBodyRelayEvent.Type => BodyProviderType.All;
}

/// <summary>
/// Event raised on an entity before it bleeds. Allows other systems to modify the amount of bleeding
/// or the amount of reduction applied to bleeding.
/// </summary>
/// <param name="Bleeding">The current amount of blood the entity is about to lose.</param>
/// <param name="Reduction">The total amount of bleeding reduction.</param>
[ByRefEvent]
public record struct BleedModifierEvent(FixedPoint2 Bleeding, FixedPoint2 Reduction);

/// <summary>
/// Event raised on an entity to request the total of bleeding from all relevant sources.
/// </summary>
/// <param name="Bleeding">The current total bleeding.</param>
/// <param name="Decay">How fast the total bleeding decreases on its own, per second.</param>
/// <param name="NextChange">
/// The earliest time the bleeding stops decreasing linearly (e.g. a wound stops bleeding),
/// after which it has to be requested again. Null if it never changes on its own.
/// </param>
[ByRefEvent]
public record struct GetBleedingEvent(FixedPoint2 Bleeding, float Decay = 0f, TimeSpan? NextChange = null) : IBodyRelayEvent, IWoundRelayEvent
{
    BodyProviderType IBodyRelayEvent.Type => BodyProviderType.All;
    ProtoId<DamageTypePrototype>? IWoundRelayEvent.Type => null;

    /// <summary>
    /// Returns a copy with the given source of bleeding added.
    /// </summary>
    public readonly GetBleedingEvent Add(FixedPoint2 bleeding, float decay, TimeSpan? nextChange)
    {
        var next = NextChange;
        if (nextChange is { } change && (next is null || change < next))
            next = change;

        return new GetBleedingEvent(Bleeding + bleeding, Decay + decay, next);
    }

    /// <summary>
    /// Returns a copy with the bleeding of another request added.
    /// </summary>
    public readonly GetBleedingEvent Add(GetBleedingEvent other)
    {
        return Add(other.Bleeding, other.Decay, other.NextChange);
    }
}

/// <summary>
/// Event raised on an entity to request the total of blood regeneration from all relevant sources.
/// </summary>
[ByRefEvent]
public record struct GetBloodReductionEvent(FixedPoint2 Reduction) : IBodyRelayEvent
{
    BodyProviderType IBodyRelayEvent.Type => BodyProviderType.All;
}

/// <summary>
/// Event relayed to all wounds of a body to change their bleeding proportionally.
/// </summary>
/// <param name="Bleeding">The relative change of each wound's bleeding. </param>
[ByRefEvent]
public record struct ModifyBleedingEvent(FixedPoint2 Bleeding) : IWoundRelayEvent
{
    ProtoId<DamageTypePrototype>? IWoundRelayEvent.Type => null;
}
