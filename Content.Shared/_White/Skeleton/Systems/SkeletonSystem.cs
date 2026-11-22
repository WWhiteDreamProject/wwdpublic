using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Skeleton.Components;
using Content.Shared._White.Wounds;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Skeleton.Systems;

public sealed class SkeletonSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkeletonProviderComponent, BeforeRelayWoundableDamageEvent>(OnBeforeWoundableRelayDamage);
        SubscribeLocalEvent<SkeletonProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<SkeletonProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<SkeletonProviderComponent, GetModifiedDamageEvent>(OnGetModifiedDamage);
        SubscribeLocalEvent<SkeletonProviderComponent, WoundSeverityChangedEvent>(OnWoundSeverityChanged);
    }

    #region Event Handling

    private void OnBeforeWoundableRelayDamage(Entity<SkeletonProviderComponent> ent, ref BeforeRelayWoundableDamageEvent args)
    {
        if (args.IgnoreResistances)
            return;

        args.Damage = ReduceByStrength(args.Damage, ent.Comp.Strength);
    }

    private void OnGotInsertedIntoParent(Entity<SkeletonProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        ent.Comp.Parent = args.Parent;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Parent));

        var ev = new SkeletonSeverityChangedEvent(ent.Comp.Severity, ent);
        RaiseLocalEvent(args.Parent, ref ev);
    }

    private void OnGotRemovedFromParent(Entity<SkeletonProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        ent.Comp.Parent = null;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Parent));

        var ev = new SkeletonSeverityChangedEvent(WoundSeverity.None, ent);
        RaiseLocalEvent(args.Parent, ref ev);
    }

    private void OnGetModifiedDamage(Entity<SkeletonProviderComponent> ent, ref GetModifiedDamageEvent args)
    {
        args.Result = ReduceByStrength(args.Result, ent.Comp.Strength);
    }

    private void OnWoundSeverityChanged(Entity<SkeletonProviderComponent> ent, ref WoundSeverityChangedEvent args)
    {
        ent.Comp.Severity = args.Severity;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Severity));

        var ev = new SkeletonSeverityChangedEvent(args.Severity, ent);
        RaiseLocalEvent(ent, ref ev);

        UpdateStrength(ent);

        if (ent.Comp.Parent is not { } parent)
            return;

        RaiseLocalEvent(parent, ref ev);
    }

    #endregion

    #region Private API

    /// <summary>
    /// Subtracts a flat strength from the total positive damage, split proportionally between damage types.
    /// Never turns damage into healing; negative entries are kept as is.
    /// </summary>
    private static DamageSpecifier ReduceByStrength(DamageSpecifier damage, FixedPoint2 strength)
    {
        if (strength <= FixedPoint2.Zero)
            return damage;

        var positive = DamageSpecifier.GetPositive(damage);
        var total = positive.GetTotal();
        if (total <= FixedPoint2.Zero)
            return damage;

        var result = DamageSpecifier.GetNegative(damage);
        if (total <= strength)
            return result;

        result += positive * ((total - strength) / total);
        result.TrimZeros();
        return result;
    }

    public void UpdateStrength(Entity<SkeletonProviderComponent> ent)
    {
        var strength = ent.Comp.Thresholds.GetValueOrDefault(ent.Comp.Severity);
        if (ent.Comp.Strength == strength)
            return;

        ent.Comp.Strength = strength;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Strength));
    }

    #endregion
}

/// <summary>
/// Event raised on entity after changing his skeleton severity.
/// </summary>
[ByRefEvent]
public record struct SkeletonSeverityChangedEvent(WoundSeverity Severity, Entity<SkeletonProviderComponent> Provider);
