using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared.Alert;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._White.Body.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem : EntitySystem
{
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;

    private EntityQuery<BloodstreamComponent> _bloodstreamQuery;

    public override void Initialize()
    {
        base.Initialize();

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();

        InitializeBloodstream();
        InitializeBodyPart();
        InitializeGenerator();
        InitializeWound();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (bloodstream.AdjustedUpdateInterval == TimeSpan.Zero || curTime < bloodstream.NextUpdate)
                continue;

            UpdateBloodstream((uid, bloodstream));
        }
    }

    #region Public API

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
