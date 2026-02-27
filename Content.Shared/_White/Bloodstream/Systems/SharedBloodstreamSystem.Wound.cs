using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeWound()
    {
        SubscribeLocalEvent<BleedingWoundComponent, WoundDamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<BleedingWoundComponent, WoundRelayedEvent<GetBleedingEvent>>(OnGetBleeding);
    }

    #region Event Handling

    private void OnDamageChange(Entity<BleedingWoundComponent> ent, ref WoundDamageChangedEvent args)
    {
        if (args.Damage <= 0)
            return;

        if (args.Wound.Damage < ent.Comp.StartsBleedingAbove)
            return;

        ModifyBleed(ent.AsNullable(), args.Damage * ent.Comp.BleedingCoefficient);
    }

    private void OnGetBleeding(Entity<BleedingWoundComponent> ent, ref WoundRelayedEvent<GetBleedingEvent> args)
    {
        var ratio = 1f;
        if (args.Wound.Damage < ent.Comp.RequiresTendingAbove)
        {
            var expiresAfter = TimeSpan.FromSeconds((args.Wound.Damage * ent.Comp.BleedingDurationCoefficient).Double());

            if (args.Wound.WoundedAt + expiresAfter <= _gameTiming.CurTime)
                return;

            var expiryTime = (args.Wound.WoundedAt + expiresAfter - _gameTiming.CurTime).TotalSeconds;
            ratio = (float)(expiryTime / expiresAfter.TotalSeconds);
        }

        ent.Comp.Bleeding *= ratio;
        DirtyField(ent, ent.Comp, nameof(BleedingWoundComponent.Bleeding));

        args.Args = new (args.Args.Bleeding + ent.Comp.Bleeding);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Tries to make a wound bleed more or less.
    /// </summary>
    public FixedPoint2 ModifyBleed(Entity<BleedingWoundComponent?> ent, FixedPoint2 amount)
    {
        if (!_woundQuery.Resolve(ent, ref ent.Comp))
            return 0f;

        var oldBleeding = ent.Comp.Bleeding;

        ent.Comp.Bleeding = FixedPoint2.Clamp(ent.Comp.Bleeding + amount, 0f, ent.Comp.MaxBleeding);
        DirtyField(ent, ent.Comp, nameof(BleedingWoundComponent.Bleeding));

        return ent.Comp.Bleeding - oldBleeding;
    }

    #endregion
}
