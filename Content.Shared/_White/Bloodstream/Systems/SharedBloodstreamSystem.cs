using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Chemistry.Reagent;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Gibbable.Systems;
using Content.Shared._White.Threshold;
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

        if (args.Entity == entity.Comp.TemporarySolution?.Owner)
            entity.Comp.TemporarySolution = null;
    }

    private void OnGibbedBeforeDeletion(Entity<BloodstreamComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        SpillAllSolutions(ent.AsNullable());
    }

    private void OnMapInit(Entity<BloodstreamComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.CurrentUpdateInterval;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.NextUpdate));
    }

    private void OnRejuvenate(Entity<BloodstreamComponent> ent, ref RejuvenateEvent args)
    {
        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out _))
            return;

        SolutionContainer.RemoveAllSolution(ent.Comp.Solution.Value);
        TryModifyBloodLevel(ent.AsNullable(), ent.Comp.CurrentMaxVolume - GetBloodAmount(ent.AsNullable()));

        UpdateBloodstream(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (bloodstream.CurrentUpdateInterval == TimeSpan.Zero || _gameTiming.CurTime < bloodstream.NextUpdate)
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

        var amount = FixedPoint2.Zero;
        foreach (var reagent in solution.Contents)
        {
            if (!BloodCompatible(ent.Comp, reagent.Reagent))
                continue;

            amount += reagent.Quantity;
        }

        if (amount == FixedPoint2.Zero)
            return true;

        OnAmountChanged((ent, ent.Comp), amount);
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
        var acceptedQuantity = FixedPoint2.Zero;

        amount = FixedPoint2.Min(amount, ent.Comp.CurrentMaxVolume - ent.Comp.Amount);

        if (amount > 0 && !SolutionContainer.TryAddReagent(ent.Comp.Solution.Value, ent.Comp.Reagent, amount, out acceptedQuantity, data: bloodData))
            return false;

        if (amount < 0)
            acceptedQuantity = -SolutionContainer.RemoveReagent(ent.Comp.Solution.Value, ent.Comp.Reagent, amount, bloodData);

        OnAmountChanged((ent, ent.Comp), acceptedQuantity);

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

        ent.Comp.Reagent = reagent;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Reagent));

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var bloodSolution))
            return;

        var currentVolume = bloodSolution.RemoveReagent(ent.Comp.Reagent, bloodSolution.Volume, ignoreReagentData: true);
        if (currentVolume <= 0)
            return;

        SolutionContainer.TryAddReagent(ent.Comp.Solution.Value, ent.Comp.Reagent, currentVolume, null, GetEntityBloodData(ent));
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
            _ => false,
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
    /// If the blood amount was changed, this function should be called.
    /// </summary>
    /// <remarks>
    /// This updates cached blood amount information.
    /// </remarks>
    private void OnAmountChanged(Entity<BloodstreamComponent> ent, FixedPoint2 amount)
    {
        if (ent.Comp.Amount + amount < FixedPoint2.Zero)
            amount = -ent.Comp.Amount;

        if (amount == FixedPoint2.Zero)
            return;

        ent.Comp.Amount += amount;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Amount));

        ent.Comp.Level = (ent.Comp.Amount / ent.Comp.CurrentMaxVolume).Float();
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Level));

        RaiseLocalEvent(ent, new BloodAmountChangedEvent(amount, ent.Comp.Level));
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

    private void UpdateBleeding(Entity<BloodstreamComponent> ent)
    {
        var getBleedingEv = new GetBleedingEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref getBleedingEv);

        var bleedingDelta = getBleedingEv.Bleeding - ent.Comp.Bleeding;
        if (bleedingDelta == 0)
            return;

        ent.Comp.Bleeding += bleedingDelta;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.Bleeding));

        RaiseLocalEvent(ent, new BleedingChangedEvent(bleedingDelta));

        UpdateAlert(ent);

        var bleedingLevel = ent.Comp.BleedingThresholds.HighestMatch(ent.Comp.Bleeding) ?? BleedingLevel.Zero;
        if (ent.Comp.BleedingLevel == bleedingLevel)
            return;

        ent.Comp.BleedingLevel = bleedingLevel;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BleedingLevel));

        RaiseLocalEvent(ent, new BleedingLevelChangedEvent(bleedingLevel));
    }

    private void UpdateBleedOut(Entity<BloodstreamComponent> ent)
    {
        if (ent.Comp.Bleeding == 0)
            return;

        var ev = new BleedModifierEvent(ent.Comp.Bleeding, 0);
        RaiseLocalEvent(ent, ref ev);

        TryBleedOut(ent.AsNullable(), ev.Bleeding);

        // TryModifyBleedAmount(ent.AsNullable(), -ev.BleedReductionAmount); TODO
    }

    private void UpdateBloodstream(Entity<BloodstreamComponent> ent)
    {
        ent.Comp.NextUpdate += ent.Comp.CurrentUpdateInterval;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.NextUpdate));

        if (ent.Comp.Amount < ent.Comp.CurrentMaxVolume)
        {
            var getReductionEv = new GetBloodReductionEvent(FixedPoint2.Zero);
            RaiseLocalEvent(ent, ref getReductionEv);

            TryModifyBloodLevel(ent.AsNullable(), getReductionEv.Reduction);
        }

        UpdateBleeding(ent);

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
                Group = ent.Comp.Group,
            },
            new DnaData
            {
                DNA = TryComp<DnaComponent>(ent, out var dnaComponent)
                    ? dnaComponent.DNA
                    : Loc.GetString("forensics-dna-unknown"),
            },
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
public record struct BleedingChangedEvent(FixedPoint2 Bleeding);

/// <summary>
/// Event raised on an entity after its bleeding level has been changed.
/// </summary>
/// <param name="Level">The new bleeding level.</param>
/// <param name="Location">The specific body location when bleeding level changed.</param>
public record struct BleedingLevelChangedEvent(BleedingLevel Level, BodyProviderType Location = BodyProviderType.All);

/// <summary>
/// Event raised on an entity after its blood level has changed.
/// </summary>
/// <param name="Amount">The amount by which the blood has changed.</param>
/// <param name="Level">The current blood level.</param>
public record struct BloodAmountChangedEvent(FixedPoint2 Amount, float Level) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised on an entity before it bleeds. Allows other systems to modify the amount of bleeding
/// or the amount of reduction applied to bleeding.
/// </summary>
/// <param name="Bleeding">The current amount of blood the entity is about to lose.</param>
/// <param name="BleedReductionAmount">The total amount of bleeding reduction.</param>
[ByRefEvent]
public record struct BleedModifierEvent(FixedPoint2 Bleeding, FixedPoint2 BleedReductionAmount);

/// <summary>
/// Event raised on an entity to request the total of bleeding from all relevant sources.
/// </summary>
[ByRefEvent]
public record struct GetBleedingEvent(FixedPoint2 Bleeding) : IWoundRelayEvent
{
    public ProtoId<DamageTypePrototype>? Type { get; } = null;
}

/// <summary>
/// Event raised on an entity to request the total of blood reductions from all relevant sources.
/// </summary>
[ByRefEvent]
public record struct GetBloodReductionEvent(FixedPoint2 Reduction);
