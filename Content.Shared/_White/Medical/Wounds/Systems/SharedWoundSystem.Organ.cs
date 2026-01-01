using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Wounds.Components;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    private void InitializeOrgan()
    {
        SubscribeLocalEvent<WoundableOrganComponent, RejuvenateEvent>(OnOrganRejuvenate);
    }

    #region Event Handling

    private void OnOrganRejuvenate(Entity<WoundableOrganComponent> woundableOrgan, ref RejuvenateEvent args)
    {
        woundableOrgan.Comp.Health = woundableOrgan.Comp.MaximumHealth;
        CheckOrganStatusThreshold((woundableOrgan, null, woundableOrgan));
    }

    #endregion

    #region Private API

    private void ApplyOrganDamage(Entity<OrganComponent?, WoundableOrganComponent?> organ, DamageSpecifier damage)
    {
        if (!Resolve(organ, ref organ.Comp1, ref organ.Comp2))
            return;

        var totalDamageDelta = FixedPoint2.Zero;

        foreach (var (damageType, damageValue) in damage.DamageDict)
        {
            if (damageValue <= 0 || !organ.Comp2.SupportedDamageType.Contains(damageType))
                continue;

            totalDamageDelta += damageValue;
        }

        if (totalDamageDelta  == FixedPoint2.Zero)
            return;

        organ.Comp2.Health = FixedPoint2.Max(organ.Comp2.Health - totalDamageDelta, FixedPoint2.Zero);
        DirtyField(organ, organ.Comp2, nameof(WoundableOrganComponent.Health));

        RaiseLocalEvent(organ, new OrganHealthChangedEvent(totalDamageDelta));

        CheckOrganStatusThreshold(organ);
    }

    private void CheckOrganStatusThreshold(Entity<OrganComponent?, WoundableOrganComponent?> organ)
    {
        if (!Resolve(organ, ref organ.Comp1, ref organ.Comp2))
            return;

        var organStatus = organ.Comp2.OrganStatusThresholds.LowestMatch(organ.Comp2.Health) ?? OrganStatus.Healthy;
        if (organ.Comp2.CurrentOrganStatusThreshold == organStatus)
            return;

        organ.Comp2.CurrentOrganStatusThreshold = organStatus;
        DirtyField(organ, organ.Comp2, nameof(WoundableOrganComponent.CurrentOrganStatusThreshold));

        var ev = new OrganStatusChangedEvent((organ, organ.Comp2), organStatus);
        RaiseLocalEvent(organ, ev);

        if (organ.Comp1.Body.HasValue)
            RaiseLocalEvent(organ.Comp1.Body.Value, ev);

        if (organ.Comp1.Parent.HasValue)
            RaiseLocalEvent(organ.Comp1.Parent.Value, ev);
    }

    #endregion
}
