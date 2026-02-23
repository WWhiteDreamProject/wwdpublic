using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Components;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;

namespace Content.Shared._White.Body.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    private void InitializeOrgan() =>
        SubscribeLocalEvent<WoundableOrganComponent, OrganRelayedEvent<RejuvenateEvent>>(OnOrganRejuvenate);

    #region Event Handling

    private void OnOrganRejuvenate(Entity<WoundableOrganComponent> ent, ref OrganRelayedEvent<RejuvenateEvent> args) =>
        ChangeOrganDamage((ent, null, ent), ent.Comp.Health - ent.Comp.MaximumHealth);

    #endregion

    #region Private API

    private void CheckOrganStatusThreshold(Entity<OrganComponent?, WoundableOrganComponent?> organ)
    {
        if (!Resolve(organ, ref organ.Comp1, ref organ.Comp2))
            return;

        var organStatus = organ.Comp2.OrganStatusThresholds.LowestMatch(organ.Comp2.Health) ?? OrganStatus.Healthy;
        if (organ.Comp2.CurrentOrganStatusThreshold == organStatus)
            return;

        organ.Comp2.CurrentOrganStatusThreshold = organStatus;
        DirtyField(organ, organ.Comp2, nameof(WoundableOrganComponent.CurrentOrganStatusThreshold));

        if (organStatus == OrganStatus.Dead)
            _body.SetOrganEnable(organ, false);

        var ev = new OrganStatusChangedEvent((organ, organ.Comp1, organ.Comp2), organStatus);
        RaiseLocalEvent(organ, ev);

        if (organ.Comp1.Body.HasValue)
            RaiseLocalEvent(organ.Comp1.Body.Value, ev);

        if (organ.Comp1.Parent.HasValue)
            RaiseLocalEvent(organ.Comp1.Parent.Value, ev);
    }

    #endregion

    #region Public API

    public void ChangeOrganDamage(Entity<OrganComponent?, WoundableOrganComponent?> organ, FixedPoint2 damage)
    {
        if (!Resolve(organ, ref organ.Comp1, ref organ.Comp2))
            return;

        organ.Comp2.Health = FixedPoint2.Clamp(organ.Comp2.Health - damage, FixedPoint2.Zero, organ.Comp2.MaximumHealth);
        DirtyField(organ, organ.Comp2, nameof(WoundableOrganComponent.Health));

        RaiseLocalEvent(organ, new OrganHealthChangedEvent((organ, organ.Comp1, organ.Comp2), damage));

        CheckOrganStatusThreshold(organ);
    }

    public void ApplyOrganDamage(Entity<OrganComponent?, WoundableOrganComponent?> organ, DamageSpecifier damage)
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

        ChangeOrganDamage(organ, totalDamageDelta);
    }

    #endregion
}
