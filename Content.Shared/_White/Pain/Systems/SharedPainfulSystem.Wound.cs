using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Pain.Components;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Pain.Systems;

public abstract partial class SharedPainfulSystem
{
    private void InitializeWound()
    {
        SubscribeLocalEvent<PainfulWoundComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PainfulWoundComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<PainfulWoundComponent, WoundCreatedEvent>(OnWoundCreated);
        SubscribeLocalEvent<PainfulWoundComponent, WoundRelayedEvent<GetPainEvent>>(OnGetPain);
    }

    #region Event Handling

    private void OnShutdown(Entity<PainfulWoundComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Body is not { } body)
            return;

        UpdatePain(body);
    }

    private void OnDamageChanged(Entity<PainfulWoundComponent> ent, ref DamageChangedEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        var totalDamage = args.Damageable.TotalDamage;

        ent.Comp.LastingPain = ent.Comp.PainCoefficients * totalDamage;
        DirtyField(ent, ent.Comp, nameof(PainfulWoundComponent.LastingPain));

        var freshPain = GetWoundFreshPain(ent.AsNullable());
        var damage = args.Damage?.GetTotal() ?? FixedPoint2.Zero;

        // A new hit adds its own acute pain; healing can't leave more acute pain than the wound itself could cause.
        freshPain = damage > FixedPoint2.Zero
            ? freshPain + ent.Comp.FreshPainCoefficients * damage
            : FixedPoint2.Min(freshPain, ent.Comp.FreshPainCoefficients * totalDamage);

        ent.Comp.FreshPain = FixedPoint2.Max(freshPain, FixedPoint2.Zero);
        DirtyField(ent, ent.Comp, nameof(PainfulWoundComponent.FreshPain));

        ent.Comp.FreshPainSince = GameTiming.CurTime;
        DirtyField(ent, ent.Comp, nameof(PainfulWoundComponent.FreshPainSince));

        if (ent.Comp.Body is not { } body)
            return;

        UpdatePain(body);
    }

    private void OnWoundCreated(Entity<PainfulWoundComponent> ent, ref WoundCreatedEvent args)
    {
        ent.Comp.Body = args.Wound.Body;
        DirtyField(ent, ent.Comp, nameof(PainfulWoundComponent.Body));
    }

    private void OnGetPain(Entity<PainfulWoundComponent> ent, ref WoundRelayedEvent<GetPainEvent> args)
    {
        // The wound is being removed, so it no longer hurts.
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
            return;

        var freshPain = GetWoundFreshPain(ent.AsNullable());
        var decay = freshPain > FixedPoint2.Zero ? (float) ent.Comp.FreshPainDecreasePerSecond : 0f;

        args.Args = args.Args.Add(ent.Comp.LastingPain + freshPain, decay);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Returns the current acute pain of the wound, taking into account that it fades over time.
    /// </summary>
    public FixedPoint2 GetWoundFreshPain(Entity<PainfulWoundComponent?> ent)
    {
        if (!_woundQuery.Resolve(ent, ref ent.Comp))
            return FixedPoint2.Zero;

        var elapsed = (GameTiming.CurTime - ent.Comp.FreshPainSince).TotalSeconds;
        var decreased = FixedPoint2.New(Math.Max(elapsed, 0d) * ent.Comp.FreshPainDecreasePerSecond);

        return FixedPoint2.Max(ent.Comp.FreshPain - decreased, FixedPoint2.Zero);
    }

    #endregion
}
