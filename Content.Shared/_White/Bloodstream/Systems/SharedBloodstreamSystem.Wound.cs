using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeWound()
    {
        SubscribeLocalEvent<BleedingWoundComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BleedingWoundComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<BleedingWoundComponent, WoundCreatedEvent>(OnWoundCreated);
        SubscribeLocalEvent<BleedingWoundComponent, WoundRelayedEvent<GetBleedingEvent>>(OnGetBleeding);
        SubscribeLocalEvent<BleedingWoundComponent, WoundRelayedEvent<ModifyBleedingEvent>>(OnModifyBleeding);
    }

    #region Event Handling

    private void OnShutdown(Entity<BleedingWoundComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Body is not { } body)
            return;

        UpdateBleeding(body);
    }

    private void OnDamageChange(Entity<BleedingWoundComponent> ent, ref DamageChangedEvent args)
    {
        ent.Comp.Damage = args.Damageable.TotalDamage;
        DirtyField(ent, ent.Comp, nameof(BleedingWoundComponent.Damage));

        var damage = args.Damage.GetTotal();
        if (damage < 0 || args.Damageable.TotalDamage < ent.Comp.StartsBleedingAbove)
            return;

        SetWoundBleeding(ent, GetWoundBleeding(ent.AsNullable()) + damage * ent.Comp.BleedingCoefficient);
    }

    private void OnWoundCreated(Entity<BleedingWoundComponent> ent, ref WoundCreatedEvent args)
    {
        ent.Comp.Body = args.Wound.Body;
        DirtyField(ent, ent.Comp, nameof(BleedingWoundComponent.Body));
    }

    private void OnGetBleeding(Entity<BleedingWoundComponent> ent, ref WoundRelayedEvent<GetBleedingEvent> args)
    {
        var bleeding = GetWoundBleeding(ent.AsNullable());
        if (bleeding <= FixedPoint2.Zero)
            return;

        if (ent.Comp.BleedingDuration is not { } duration || duration <= TimeSpan.Zero)
        {
            args.Args = args.Args.Add(bleeding, 0f, null);
            return;
        }

        var decay = ent.Comp.Bleeding.Float() / (float) duration.TotalSeconds;
        args.Args = args.Args.Add(bleeding, decay, ent.Comp.BleedingSince + duration);
    }

    private void OnModifyBleeding(Entity<BleedingWoundComponent> ent, ref WoundRelayedEvent<ModifyBleedingEvent> args)
    {
        var bleeding = GetWoundBleeding(ent.AsNullable());
        if (bleeding <= FixedPoint2.Zero)
            return;

        SetWoundBleeding(ent, bleeding + args.Args.Bleeding / args.Count, resetDuration: false, updateBody: false);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Tries to make a wound bleed more or less.
    /// The wound still stops bleeding on its own at the same time as before.
    /// </summary>
    /// <returns>The actual change of the wound's bleeding.</returns>
    public FixedPoint2 ModifyBleed(Entity<BleedingWoundComponent?> ent, FixedPoint2 amount)
    {
        if (!_woundQuery.Resolve(ent, ref ent.Comp))
            return 0f;

        var bleeding = GetWoundBleeding(ent.AsNullable());
        SetWoundBleeding((ent, ent.Comp), bleeding + amount, resetDuration: false);

        return ent.Comp.Bleeding - bleeding;
    }

    /// <summary>
    /// Returns the current bleeding of the wound, taking into account that it stops bleeding over time.
    /// </summary>
    public FixedPoint2 GetWoundBleeding(Entity<BleedingWoundComponent?> ent)
    {
        if (!_woundQuery.Resolve(ent, ref ent.Comp))
            return 0f;

        if (ent.Comp.BleedingDuration is not { } duration)
            return ent.Comp.Bleeding;

        var elapsed = _gameTiming.CurTime - ent.Comp.BleedingSince;
        if (duration <= TimeSpan.Zero || elapsed >= duration)
            return FixedPoint2.Zero;

        var ratio = 1f - (float) (elapsed.TotalSeconds / duration.TotalSeconds);
        return ent.Comp.Bleeding * ratio;
    }

    #endregion

    #region Private API

    /// <summary>
    /// Sets the wound's bleeding starting from now and notifies the body.
    /// </summary>
    /// <param name="ent">The wound.</param>
    /// <param name="bleeding">The new bleeding of the wound.</param>
    /// <param name="resetDuration">
    /// True to recalculate how long the wound bleeds from its damage (a new injury).
    /// False to keep the moment it stops bleeding on its own (e.g. medicine changing the bleeding).
    /// </param>
    /// <param name="updateBody">Whether to recollect the bleeding of the body right away.</param>
    private void SetWoundBleeding(Entity<BleedingWoundComponent> ent, FixedPoint2 bleeding, bool resetDuration = true, bool updateBody = true)
    {
        var curTime = _gameTiming.CurTime;

        TimeSpan? duration;
        if (resetDuration)
        {
            duration = ent.Comp.Damage >= ent.Comp.RequiresTendingAbove
                ? null
                : TimeSpan.FromSeconds((ent.Comp.Damage * ent.Comp.BleedingDurationCoefficient).Double());
        }
        else
        {
            // Keep the end of the bleeding where it was; a wound that requires tending still does.
            duration = ent.Comp.BleedingDuration is { } oldDuration
                ? ent.Comp.BleedingSince + oldDuration - curTime
                : null;

            if (duration <= TimeSpan.Zero)
                bleeding = FixedPoint2.Zero;
        }

        ent.Comp.Bleeding = FixedPoint2.Clamp(bleeding, FixedPoint2.Zero, ent.Comp.MaxBleeding);
        DirtyField(ent, ent.Comp, nameof(BleedingWoundComponent.Bleeding));

        ent.Comp.BleedingSince = curTime;
        DirtyField(ent, ent.Comp, nameof(BleedingWoundComponent.BleedingSince));

        ent.Comp.BleedingDuration = duration;
        DirtyField(ent, ent.Comp, nameof(BleedingWoundComponent.BleedingDuration));

        if (!updateBody || ent.Comp.Body is not { } body)
            return;

        UpdateBleeding(body);
    }

    #endregion
}
