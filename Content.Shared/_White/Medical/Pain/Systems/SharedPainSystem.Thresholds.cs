using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Pain.Components;
using Content.Shared._White.Threshold;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._White.Medical.Pain.Systems;

public abstract partial class SharedPainSystem
{
    private void InitializeThresholds()
    {
        SubscribeLocalEvent<PainThresholdsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PainThresholdsComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PainThresholdsComponent, UpdateMobStateEvent>(OnUpdateMobState);
        SubscribeLocalEvent<PainThresholdsComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<PainThresholdsComponent, AfterPainChangedEvent>(OnAfterPainChanged);
    }

    #region Event Handling

    private void OnStartup(Entity<PainThresholdsComponent> painThresholds, ref ComponentStartup args)
    {
        var currentPain = GetCurrentPain(painThresholds.Owner);

        _alerts.ShowAlert(painThresholds, painThresholds.Comp.BodyStatusAlert);

        ChekMobStateThresholds(painThresholds.AsNullable(), currentPain);
        ChekPainLevelThresholds(painThresholds.AsNullable(), currentPain);
        UpdateAlerts(painThresholds.AsNullable(), currentPain);
    }

    private void OnShutdown(Entity<PainThresholdsComponent> painThresholds, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(painThresholds, painThresholds.Comp.AlertCategory);
        _alerts.ClearAlert(painThresholds, painThresholds.Comp.BodyStatusAlert);
    }

    private void OnUpdateMobState(Entity<PainThresholdsComponent> painThresholds, ref UpdateMobStateEvent args)
    {
        args.State = painThresholds.Comp.CurrentMobStateThreshold;
    }

    private void OnMobStateChanged(Entity<PainThresholdsComponent> painThresholds, ref MobStateChangedEvent args)
    {
        var currentPain = GetCurrentPain(painThresholds.Owner);
        UpdateAlerts(painThresholds.Owner, currentPain);
    }

    private void OnAfterPainChanged(Entity<PainThresholdsComponent> painThresholds, ref AfterPainChangedEvent args)
    {
        ChekMobStateThresholds(painThresholds.AsNullable(), args.CurrentPain);
        ChekPainLevelThresholds(painThresholds.AsNullable(), args.CurrentPain);
        UpdateAlerts(painThresholds.AsNullable(), args.CurrentPain);
    }

    #endregion

    #region Private API

    private void ChekMobStateThresholds(Entity<PainThresholdsComponent?> painThresholds, FixedPoint2 currentPain)
    {
        if (!Resolve(painThresholds, ref painThresholds.Comp)
            || painThresholds.Comp.CurrentMobStateThreshold == MobState.Dead)
            return;

        var mobState = painThresholds.Comp.MobStateThresholds.HighestMatch(currentPain) ?? MobState.Alive;
        if (mobState == painThresholds.Comp.CurrentMobStateThreshold)
            return;

        painThresholds.Comp.CurrentMobStateThreshold = mobState;
        DirtyField(painThresholds, nameof(PainThresholdsComponent.CurrentMobStateThreshold));

        _mobState.UpdateMobState(painThresholds);
    }

    private void ChekPainLevelThresholds(Entity<PainThresholdsComponent?> painThresholds, FixedPoint2 currentPain)
    {
        if (!Resolve(painThresholds, ref painThresholds.Comp)
            || painThresholds.Comp.CurrentMobStateThreshold == MobState.Dead)
            return;

        var painLevel = painThresholds.Comp.PainLevelThresholds.HighestMatch(currentPain) ?? PainLevel.Zero;
        if (painLevel == painThresholds.Comp.CurrentPainLevelThreshold)
            return;

        painThresholds.Comp.CurrentPainLevelThreshold = painLevel;
        DirtyField(painThresholds, nameof(PainThresholdsComponent.CurrentPainLevelThreshold));

        if (!painThresholds.Comp.PainEffects.TryGetValue(painLevel, out var effects))
            return;

        var effectsArgs = new EntityEffectBaseArgs(painThresholds, EntityManager);
        foreach (var effect in effects)
            effect.Effect(effectsArgs);
    }

    private void UpdateAlerts(Entity<PainThresholdsComponent?> painThresholds, FixedPoint2 currentPain)
    {
        if (!Resolve(painThresholds, ref painThresholds.Comp))
            return;

        if (!painThresholds.Comp.StateAlertDict.TryGetValue(painThresholds.Comp.CurrentMobStateThreshold, out var currentAlert))
        {
            Log.Error($"No alert alert for mob state {painThresholds.Comp.CurrentMobStateThreshold} for entity {ToPrettyString(painThresholds)}");
            return;
        }

        if (!_alerts.TryGet(currentAlert, out var alertPrototype))
        {
            Log.Error($"Invalid alert type {currentAlert}");
            return;
        }

        if (alertPrototype.SupportsSeverity)
        {
            var severity = _alerts.GetMinSeverity(currentAlert);
            if (TryGetNextState(painThresholds.Comp.MobStateThresholds, painThresholds.Comp.CurrentMobStateThreshold, out var nextState)
                && TryGetThresholdFromState(painThresholds.Comp.MobStateThresholds, nextState.Value, out var threshold)
                && threshold != 0)
            {
                threshold = FixedPoint2.Clamp(currentPain / threshold.Value, 0, 1);

                severity = (short) MathF.Round(
                    MathHelper.Lerp(
                        _alerts.GetMinSeverity(currentAlert),
                        _alerts.GetMaxSeverity(currentAlert),
                        threshold.Value.Float()));
            }

            _alerts.ShowAlert(painThresholds, currentAlert, severity);
        }
        else
        {
            _alerts.ShowAlert(painThresholds, currentAlert);
        }
    }

    private void SetPainStatus(Entity<PainThresholdsComponent?> painThresholds, Entity<BodyPartComponent?> bodyPart, PainLevel painLevel)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return;

        SetPainStatus(painThresholds, bodyPart.Comp.Type, painLevel);
    }

    private void SetPainStatus(Entity<PainThresholdsComponent?> painThresholds, BodyPartType bodyPartType, PainLevel painLevel)
    {
        if (!Resolve(painThresholds, ref painThresholds.Comp)
            || !painThresholds.Comp.PainStatus.ContainsKey(bodyPartType))
            return;

        painThresholds.Comp.PainStatus[bodyPartType] = painLevel;
        Dirty(painThresholds);
    }

    private MobState? GetNextState(SortedDictionary<FixedPoint2, MobState> thresholds, MobState currentState)
    {
        foreach (var state in thresholds.Values)
        {
            if (state <= currentState)
                continue;

            return state;
        }

        return null;
    }

    private FixedPoint2? GetThresholdFromState(SortedDictionary<FixedPoint2, MobState> thresholds, MobState currentState)
    {
        foreach (var (threshold, state) in thresholds)
        {
            if (state == currentState)
                return threshold;
        }

        return null;
    }

    #endregion

    #region Public API

    public bool TryGetNextState(SortedDictionary<FixedPoint2, MobState> thresholds, MobState currentState, [NotNullWhen(true)] out MobState? state)
    {
        state = GetNextState(thresholds, currentState);
        return state.HasValue;
    }

    public bool TryGetThresholdFromState(
        SortedDictionary<FixedPoint2, MobState> thresholds,
        MobState currentState,
        [NotNullWhen(true)] out FixedPoint2? threshold
    )
    {
        threshold = GetThresholdFromState(thresholds, currentState);
        return threshold.HasValue;
    }

    #endregion
}
