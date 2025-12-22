using Content.Shared._White.Medical.Pain.Components;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Medical.Pain.Systems;

public abstract partial class SharedPainSystem
{
    private void InitializeBodyPart() => SubscribeLocalEvent<PainfulBodyPartComponent, GetPainEvent>(OnBodyPartGetPain);

    #region Event Handling

    private void OnBodyPartGetPain(Entity<PainfulBodyPartComponent> painfulBodyPart, ref GetPainEvent args)
    {
        painfulBodyPart.Comp.Pain = FixedPoint2.Zero;
        foreach (var wound in _wound.GetWounds<PainfulWoundComponent>(painfulBodyPart))
            painfulBodyPart.Comp.Pain += GetWoundPain(wound.AsNullable());

        painfulBodyPart.Comp.PainLevel = painfulBodyPart.Comp.Thresholds.HighestMatch(painfulBodyPart.Comp.Pain) ?? PainLevel.None;
        Dirty(painfulBodyPart);

        SetPainStatus(args.Painful.Owner, painfulBodyPart.Owner, painfulBodyPart.Comp.PainLevel);

        args.Pain += painfulBodyPart.Comp.Pain;
    }

    #endregion
}
