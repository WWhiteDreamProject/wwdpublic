using Content.Shared._White.Medical.Pain.Components;
using Content.Shared._White.Medical.Wounds.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;

namespace Content.Shared._White.Medical.Pain.Systems;

public abstract partial class SharedPainSystem
{
    private void InitializePainful()
    {
        SubscribeLocalEvent<PainfulComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PainfulComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<PainfulComponent, WoundableDamageChangedEvent>(OnWoundableDamageChange);
    }

    #region Event Handling

    private void OnMapInit(Entity<PainfulComponent> painful, ref MapInitEvent args) =>
        painful.Comp.LastUpdate = GameTiming.CurTime;

    private void OnRejuvenate(Entity<PainfulComponent> painful, ref RejuvenateEvent args)
    {
        RaiseLocalEvent(painful, new AfterPainChangedEvent(painful, 0, painful.Comp.CurrentPain));
        painful.Comp.Pain = 0;
        Dirty(painful);
    }

    private void OnWoundableDamageChange(Entity<PainfulComponent> painful, ref WoundableDamageChangedEvent args) =>
        UpdatePain(painful.AsNullable(), false);

    #endregion

    #region Private API

    protected void UpdatePain(Entity<PainfulComponent?> painful, bool sync = true)
    {
        if (!Resolve(painful, ref painful.Comp) || GameTiming is { IsFirstTimePredicted: false, InPrediction: true, })
            return;

        var delta = GameTiming.CurTime - painful.Comp.LastUpdate;
        painful.Comp.LastUpdate = GameTiming.CurTime;

        var getPain = new GetPainEvent((painful, painful.Comp), FixedPoint2.Zero);
        RaiseLocalEvent(painful, ref getPain);

        var oldPain = painful.Comp.CurrentPain;

        if (painful.Comp.Pain < getPain.Pain)
        {
            var maxIncrease = delta.TotalSeconds * painful.Comp.MaxPainIncreasePerSecond;
            painful.Comp.Pain += FixedPoint2.Min(getPain.Pain - painful.Comp.Pain, maxIncrease);
        }

        if (painful.Comp.Pain > getPain.Pain)
        {
            var maxDecrease = delta.TotalSeconds * painful.Comp.MaxPainDecreasePerSecond;
            painful.Comp.Pain -= FixedPoint2.Min(painful.Comp.Pain - getPain.Pain, maxDecrease);
        }

        if (painful.Comp.Pain < 0)
            painful.Comp.Pain = 0;

        if (painful.Comp.CurrentPain == oldPain)
            return;

        RaiseLocalEvent(painful, new AfterPainChangedEvent(painful, painful.Comp.CurrentPain, oldPain), true);

        if (sync)
            Dirty(painful);
    }

    #endregion

    #region Public API

    public FixedPoint2 GetCurrentPain(Entity<PainfulComponent?> painful)
    {
        if (!Resolve(painful, ref painful.Comp))
            return FixedPoint2.Zero;

        return painful.Comp.CurrentPain;
    }

    #endregion
}
