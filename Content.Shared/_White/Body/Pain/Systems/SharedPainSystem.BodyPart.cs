using Content.Shared._White.Body.Pain.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Body.Pain.Systems;

public abstract partial class SharedPainSystem
{
    private void InitializeBodyPart() => SubscribeLocalEvent<PainfulBodyPartComponent, BodyPartRelayedEvent<GetPainEvent>>(OnBodyPartGetPain);

    #region Event Handling

    private void OnBodyPartGetPain(Entity<PainfulBodyPartComponent> painfulBodyPart, ref BodyPartRelayedEvent<GetPainEvent> args)
    {
        painfulBodyPart.Comp.Pain = FixedPoint2.Zero;
        foreach (var wound in _wound.GetWounds<PainfulWoundComponent>(painfulBodyPart, scar: true))
            painfulBodyPart.Comp.Pain += GetWoundPain(wound.AsNullable());

        painfulBodyPart.Comp.Pain = FixedPoint2.Clamp(painfulBodyPart.Comp.Pain, FixedPoint2.Zero, painfulBodyPart.Comp.MaximumPain);
        painfulBodyPart.Comp.PainLevel = painfulBodyPart.Comp.Thresholds.HighestMatch(painfulBodyPart.Comp.Pain) ?? PainLevel.Zero;
        Dirty(painfulBodyPart);

        SetPainStatus(args.Args.Painful.Owner, painfulBodyPart.Owner, painfulBodyPart.Comp.PainLevel);

        args.Args = args.Args with { Pain = args.Args.Pain + painfulBodyPart.Comp.Pain, };
    }

    #endregion
}
