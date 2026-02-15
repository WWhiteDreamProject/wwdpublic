using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Wounds.Components;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Body.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeWound() =>
        SubscribeLocalEvent<BleedingWoundComponent, WoundDamageChangedEvent>(OnWoundDamageChange);

    #region Event Handling

    private void OnWoundDamageChange(Entity<BleedingWoundComponent> ent, ref WoundDamageChangedEvent args)
    {
        if (args.Wound.Comp.Damage <= args.OldDamage
            || args.Wound.Comp.Damage < ent.Comp.StartsBleedingAbove)
            return;

        var damageAmount = args.Wound.Comp.Damage - args.OldDamage;
        TryModifyWoundBleedAmount(ent.AsNullable(), damageAmount * ent.Comp.BleedingCoefficient);
    }

    #endregion

    #region Private API

    private FixedPoint2 GetWoundBleed(Entity<WoundComponent?, BleedingWoundComponent?> wound)
    {
        if (!Resolve(wound, ref wound.Comp1, ref wound.Comp2, false))
            return 0f;

        var ratio = 1f;
        if (wound.Comp1.Damage < wound.Comp2.RequiresTendingAbove)
        {
            var expiresAfter = TimeSpan.FromSeconds((wound.Comp1.Damage * wound.Comp2.BleedingDurationCoefficient).Double());

            if (wound.Comp1.WoundedAt + expiresAfter <= _gameTiming.CurTime)
                return 0f;

            var expiryTime = (wound.Comp1.WoundedAt + expiresAfter - _gameTiming.CurTime).TotalSeconds;
            ratio = (float)(expiryTime / expiresAfter.TotalSeconds);
        }

        wound.Comp2.Bleeding *= ratio;
        Dirty(wound, wound.Comp2);

        return wound.Comp2.Bleeding;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Tries to make a wound bleed more or less.
    /// </summary>
    public FixedPoint2 TryModifyWoundBleedAmount(Entity<BleedingWoundComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0f;

        var oldBleeding = ent.Comp.Bleeding;

        ent.Comp.Bleeding = FixedPoint2.Clamp(ent.Comp.Bleeding + amount, 0f, ent.Comp.MaximumBleeding);
        Dirty(ent);

        return ent.Comp.Bleeding - oldBleeding;
    }

    #endregion
}
