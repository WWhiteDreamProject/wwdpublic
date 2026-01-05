using Content.Shared._White.Body.Pain.Components;
using Content.Shared._White.Body.Wounds.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Body.Pain.Systems;

public abstract partial class SharedPainSystem
{
    private FixedPoint2 GetWoundPain(Entity<WoundComponent?, PainfulWoundComponent?> wound)
    {
        if (!Resolve(wound, ref wound.Comp1, ref wound.Comp2))
            return FixedPoint2.Zero;

        var lastingPain = wound.Comp2.PainCoefficients * wound.Comp1.DamageAmount;
        var freshPain = wound.Comp2.FreshPainCoefficients * wound.Comp1.DamageAmount;

        var delta = GameTiming.CurTime - wound.Comp1.WoundedAt;
        freshPain = FixedPoint2.Max(0, freshPain - delta.TotalSeconds * wound.Comp2.FreshPainDecreasePerSecond);

        return lastingPain + freshPain + wound.Comp2.Pain;
    }
}
