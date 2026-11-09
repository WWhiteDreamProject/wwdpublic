using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Pain.Components;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Pain.Systems;

public abstract partial class SharedPainfulSystem
{
    private void InitializeWound()
    {
        SubscribeLocalEvent<PainfulWoundComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<PainfulWoundComponent, WoundRelayedEvent<GetPainEvent>>(OnGetPain);
    }

    #region Event Handling

    private void OnDamageChanged(Entity<PainfulWoundComponent> ent, ref DamageChangedEvent args)
    {
        ent.Comp.LastingPain = ent.Comp.PainCoefficients * args.Damageable.TotalDamage;
        DirtyField(ent, ent.Comp, nameof(PainfulWoundComponent.LastingPain));
    }

    private void OnGetPain(Entity<PainfulWoundComponent> ent, ref WoundRelayedEvent<GetPainEvent> args)
    {
        var timeDelta = GameTiming.CurTime - args.Wound.WoundedAt;
        ent.Comp.FreshPain = FixedPoint2.Max(FixedPoint2.Zero, ent.Comp.FreshPainCoefficients * args.Wound.Damage - timeDelta.TotalSeconds * ent.Comp.FreshPainDecreasePerSecond);
        DirtyField(ent, ent.Comp, nameof(PainfulWoundComponent.FreshPain));

        args.Args = new(args.Args.Pain + ent.Comp.Pain);
    }

    #endregion
}
