using Content.Server._White.Bloodstream.Components;
using Content.Server._White.Pain.Systems;
using Content.Server._White.Respirator.Systems;
using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Pain.Systems;
using Content.Shared._White.Wounds;
using Content.Shared._White.Wounds.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Bloodstream.Systems;

/// <summary>
/// Heart rate model:
/// <code>
/// drive  = blood + hypoxia + pain + damage                    (extra BPM from the sympathetic response)
/// target = rest + (max - rest) * (1 - e^(-drive / (max - rest)))   (soft saturation towards the max rate)
/// target *= bloodFailure * hypoxiaFailure                      (decompensation, 1 = fine, 0 = no rhythm)
/// rate  -> target with an exponential approach (faster rise, slower fall)
/// shown  = rate + sinus arrhythmia + random deviation          (the deviation grows with heart damage)
/// </code>
/// Event handlers only cache inputs; all calculations happen on a fixed interval in <see cref="Update"/>,
/// which avoids the feedback loop heart rate -> metabolic rate -> heart rate.
/// </summary>
public sealed class HeartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly PainfulSystem _painful = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HeartComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<HeartComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<BloodAmountChangedEvent>>(OnBloodAmountChanged);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<GetMetabolicRateEvent>>(OnGetMetabolicRate);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<PainChangedEvent>>(OnPainChanged);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<SaturationLevelChangedEvent>>(OnSaturationLevelChanged);
        SubscribeLocalEvent<HeartComponent, WoundSeverityChangedEvent>(OnWoundSeverityChanged);
    }

    #region Event Handling

    private void OnInit(Entity<HeartComponent> ent, ref ComponentInit args)
    {
        ent.Comp.PersonalRestingRate = MathF.Max(1f, ent.Comp.RestingRate + _random.NextFloat(-ent.Comp.RestingRateVariation, ent.Comp.RestingRateVariation));
        ent.Comp.SinusPhase = _random.NextFloat(0f, MathF.Tau);
        ResetRate(ent);
    }

    private void OnGotInserted(Entity<HeartComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        ent.Comp.Body = args.Body;
        ent.Comp.Beating = ent.Comp.Severity != WoundSeverity.Critical;
        ResetRate(ent);

        _metabolizer.UpdateMetabolicRate(args.Body.Owner);
    }

    private void OnGotRemoved(Entity<HeartComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        ent.Comp.BaseRate = 0f;
        ent.Comp.Rate = 0f;
        ent.Comp.TargetRate = 0f;

        _metabolizer.UpdateMetabolicRate(args.Body.Owner);
    }

    private void OnBloodAmountChanged(Entity<HeartComponent> ent, ref BodyRelayedEvent<BloodAmountChangedEvent> args)
    {
        ent.Comp.BloodLevel = args.Args.Level;
    }

    private void OnGetMetabolicRate(Entity<HeartComponent> ent, ref BodyRelayedEvent<GetMetabolicRateEvent> args)
    {
        var rate = GetMetabolicContribution(ent);
        args.Args = new(args.Args.Rate + rate);
    }

    private void OnPainChanged(Entity<HeartComponent> ent, ref BodyRelayedEvent<PainChangedEvent> args)
    {
        ent.Comp.Pain = _painful.GetPain(args.Args.Painful.AsNullable()).Float();
    }

    private void OnSaturationLevelChanged(Entity<HeartComponent> ent, ref BodyRelayedEvent<SaturationLevelChangedEvent> args)
    {
        ent.Comp.SaturationLevel = args.Args.Level;
    }

    private void OnWoundSeverityChanged(Entity<HeartComponent> ent, ref WoundSeverityChangedEvent args)
    {
        ent.Comp.Severity = args.Severity;

        if (ent.Comp.HealthFactorThresholds.TryGetValue(args.Severity, out var healthFactor))
            ent.Comp.HealthFactor = healthFactor;

        if (args.Severity != WoundSeverity.Critical || !ent.Comp.Beating)
            return;

        Arrest(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<HeartComponent>();
        while (query.MoveNext(out var uid, out var heartComp))
        {
            if (heartComp.Body is null || curTime < heartComp.NextUpdate)
                continue;

            heartComp.LastUpdate = curTime;
            heartComp.NextUpdate = curTime + heartComp.UpdateInterval;

            UpdateTargetRate((uid, heartComp));

            if (!heartComp.Beating)
            {
                if (heartComp.Severity == WoundSeverity.Critical || heartComp.TargetRate < heartComp.RestartRate)
                    continue;

                heartComp.Beating = true;
                heartComp.BaseRate = heartComp.ArrestRate;
            }

            var smoothing = heartComp.LastUpdate == TimeSpan.Zero ? (float) heartComp.UpdateInterval.TotalSeconds : (float) (curTime - heartComp.LastUpdate).TotalSeconds;
            var tau = heartComp.TargetRate > heartComp.BaseRate ? heartComp.RiseTime : heartComp.FallTime;
            var alpha = tau <= 0f ? 1f : 1f - MathF.Exp(-smoothing / tau);
            heartComp.BaseRate += (heartComp.TargetRate - heartComp.BaseRate) * alpha;

            if (heartComp.TargetRate < heartComp.ArrestRate && heartComp.BaseRate < heartComp.ArrestRate)
            {
                Arrest((uid, heartComp));
                continue;
            }

            heartComp.Rate = MathF.Max(0f, heartComp.BaseRate + GetVariability((uid, heartComp)));

            if (heartComp.Body is not { } body)
                continue;

            _metabolizer.UpdateMetabolicRate(body);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Returns the current heart rate (BPM) or 0 if the heart is not beating.
    /// </summary>
    public float GetRate(Entity<HeartComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !ent.Comp.Beating)
            return 0f;

        return ent.Comp.Rate;
    }

    #endregion

    #region Private API

    private float GetMetabolicContribution(Entity<HeartComponent> ent)
    {
        if (!ent.Comp.Beating || ent.Comp.Body is null || ent.Comp.PersonalRestingRate <= 0f)
            return 0f;

        var relative = ent.Comp.BaseRate / ent.Comp.PersonalRestingRate;
        var output = 1f - ent.Comp.DamageOutputPenalty * ent.Comp.HealthFactor;
        return MathF.Max(0f, 1f + (relative - 1f) * ent.Comp.MetabolicResponse) * output;
    }

    private float GetVariability(Entity<HeartComponent> ent)
    {
        // Weak hearts have weak oscillations.
        var scale = Math.Clamp(ent.Comp.BaseRate / ent.Comp.PersonalRestingRate, 0f, 1f);

        var sinus = ent.Comp.SinusPeriod <= 0f ? 0f : ent.Comp.SinusAmplitude * MathF.Sin((float) _gameTiming.CurTime.TotalSeconds * MathF.Tau / ent.Comp.SinusPeriod + ent.Comp.SinusPhase);

        var deviation = ent.Comp.RateDeviation + ent.Comp.ArrhythmiaDeviation * ent.Comp.HealthFactor * ent.Comp.HealthFactor;
        var noise = _random.NextFloat(-deviation, deviation);

        return (sinus + noise) * scale;
    }

    private void Arrest(Entity<HeartComponent> ent)
    {
        ent.Comp.Beating = false;
        ent.Comp.BaseRate = 0f;
        ent.Comp.Rate = 0f;

        if (ent.Comp.Body is not { } body)
            return;

        _metabolizer.UpdateMetabolicRate(body);
    }

    private void ResetRate(Entity<HeartComponent> ent)
    {
        ent.Comp.BaseRate = ent.Comp.Beating ? ent.Comp.PersonalRestingRate : 0f;
        ent.Comp.Rate = ent.Comp.BaseRate;
        ent.Comp.TargetRate = ent.Comp.BaseRate;
        ent.Comp.LastUpdate = _gameTiming.CurTime;
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void UpdateTargetRate(Entity<HeartComponent> ent)
    {
        // Sympathetic drive in extra BPM.
        ent.Comp.Drive = 0f;
        ent.Comp.Drive += ent.Comp.BloodTachycardia * Ramp(ent.Comp.BloodLevel, ent.Comp.BloodCompensationStart, ent.Comp.BloodCompensationFull);
        ent.Comp.Drive += ent.Comp.SaturationTachycardia * Ramp(ent.Comp.SaturationLevel, ent.Comp.SaturationCompensationStart, ent.Comp.SaturationCompensationFull);
        ent.Comp.Drive += ent.Comp.PainTachycardia * (ent.Comp.PainScale <= 0f ? 0f : 1f - MathF.Exp(-MathF.Max(ent.Comp.Pain, 0f) / ent.Comp.PainScale));
        ent.Comp.Drive += ent.Comp.DamageTachycardia * ent.Comp.HealthFactor;

        // Soft saturation: ~linear for small drive, asymptotically approaches MaxRate.
        var headroom = MathF.Max(ent.Comp.MaxRate - ent.Comp.PersonalRestingRate, 1f);
        var target = ent.Comp.PersonalRestingRate + headroom * (1f - MathF.Exp(-ent.Comp.Drive / headroom));

        // Decompensation: in deep shock or hypoxia the heart can no longer keep up.
        target *= 1f - Ramp(ent.Comp.BloodLevel, ent.Comp.BloodDecompensationStart, ent.Comp.BloodDecompensationEnd);
        target *= 1f - Ramp(ent.Comp.SaturationLevel, ent.Comp.SaturationDecompensationStart, ent.Comp.SaturationDecompensationEnd);

        ent.Comp.TargetRate = target;
    }

    /// <summary>
    /// Returns 0 when <paramref name="value"/> is at or beyond <paramref name="start"/>,
    /// 1 when at or beyond <paramref name="full"/>, smoothstep in between. Works in both directions.
    /// </summary>
    private static float Ramp(float value, float start, float full)
    {
        if (MathF.Abs(full - start) < float.Epsilon)
            return value <= full ? 1f : 0f;

        var t = Math.Clamp((value - start) / (full - start), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    #endregion
}
