using Content.Server._White.Bloodstream.Components;
using Content.Server._White.Pain.Systems;
using Content.Server._White.Respirator.Systems;
using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Maths;
using Content.Shared._White.Pain.Systems;
using Content.Shared._White.Wounds;
using Content.Shared._White.Wounds.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Bloodstream.Systems;

public sealed class HeartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly PainfulSystem _painful = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<HeartComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<BloodAmountChangedEvent>>(OnBloodAmountChanged);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<GetMetabolicRateEvent>>(OnGetMetabolicRate);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<MetabolicRateChangedEvent>>(OnMetabolicRateChanged);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<PainChangedEvent>>(OnPainChanged);
        SubscribeLocalEvent<HeartComponent, BodyRelayedEvent<SaturationLevelChangedEvent>>(OnSaturationLevelChanged);
        SubscribeLocalEvent<HeartComponent, WoundableSeverityChangedEvent>(OnWoundableSeverityChanged);
    }

    #region Event Handling

    private void OnGotInserted(Entity<HeartComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        ent.Comp.Body = args.Body;
        _metabolizer.UpdateMetabolicRate(args.Body);
    }

    private void OnGotRemoved(Entity<HeartComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        _metabolizer.UpdateMetabolicRate(args.Body);
    }

    private void OnBloodAmountChanged(Entity<HeartComponent> ent, ref BodyRelayedEvent<BloodAmountChangedEvent> args)
    {
        ent.Comp.BloodFactor = WhiteMath.LogisticsGrowth(1 - args.Args.Level, ent.Comp.BloodInflection, ent.Comp.BloodSteepness);
        UpdateStrain(ent);
    }

    private void OnGetMetabolicRate(Entity<HeartComponent> ent, ref BodyRelayedEvent<GetMetabolicRateEvent> args)
    {
        args.Args = new (args.Args.Rate + ent.Comp.Rate * ent.Comp.MetabolicPerBeat);
    }

    private void OnMetabolicRateChanged(Entity<HeartComponent> ent, ref BodyRelayedEvent<MetabolicRateChangedEvent> args)
    {
        ent.Comp.MetabolicFactor = WhiteMath.LogisticsGrowth(args.Args.Rate, ent.Comp.MetabolicInflection, ent.Comp.MetabolicSteepness);
        UpdateStrain(ent);
    }

    private void OnPainChanged(Entity<HeartComponent> ent, ref BodyRelayedEvent<PainChangedEvent> args)
    {
        var pain = _painful.GetPain(args.Args.Painful.AsNullable()).Float();
        ent.Comp.PainFactor = WhiteMath.LogisticsGrowth(pain, ent.Comp.MetabolicInflection, ent.Comp.MetabolicSteepness);
        UpdateStrain(ent);
    }

    private void OnSaturationLevelChanged(Entity<HeartComponent> ent, ref BodyRelayedEvent<SaturationLevelChangedEvent> args)
    {
        ent.Comp.SaturationFactor = WhiteMath.LogisticsGrowth(1 - args.Args.Level, ent.Comp.SaturationInflection, ent.Comp.SaturationSteepness);
        UpdateStrain(ent);
    }

    private void OnWoundableSeverityChanged(Entity<HeartComponent> ent, ref WoundableSeverityChangedEvent args)
    {
        if (args.Severity == WoundSeverity.Critical)
            ent.Comp.Beating = false;

        if (!ent.Comp.HealthFactorThresholds.TryGetValue(args.Severity, out var healthFactor))
            return;

        ent.Comp.HealthFactor = healthFactor;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeartComponent>();
        while (query.MoveNext(out var uid, out var heart))
        {
            if (!heart.Beating || _gameTiming.CurTime < heart.NextUpdate)
                continue;

            UpdateHeartRate((uid, heart));
        }
    }

    #endregion

    #region Private API

    private void UpdateHeartRate(Entity<HeartComponent> ent)
    {
        if (!ent.Comp.Beating || ent.Comp.Body is not {} body)
        {
            ent.Comp.Rate = 0;
            return;
        }

        var deviation = _random.Next(-ent.Comp.RateDeviation, ent.Comp.RateDeviation);
        var rate = MathF.Max(MathHelper.Lerp(0, ent.Comp.MaxRate, ent.Comp.Strain) + deviation, 0);
        ent.Comp.Rate = WhiteMath.Diff(rate - ent.Comp.Rate, (float) _gameTiming.FrameTime.TotalSeconds);

        _metabolizer.UpdateMetabolicRate(body);

        if (ent.Comp.Rate == 0)
        {
            ent.Comp.Beating = false;
            return;
        }

        ent.Comp.NextUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp.UpdateIntervalPerBeat / ent.Comp.Rate);
    }

    private void UpdateStrain(Entity<HeartComponent> ent)
    {
        if (!ent.Comp.Beating)
        {
            ent.Comp.Strain = 0f;
            return;
        }

        var rawStrain = 0f;

        rawStrain += ent.Comp.BloodFactor;
        rawStrain += ent.Comp.HealthFactor;
        rawStrain += ent.Comp.MetabolicFactor;
        rawStrain += ent.Comp.PainFactor;
        rawStrain += ent.Comp.SaturationFactor;

        var strain = WhiteMath.LogisticsGrowth(rawStrain, ent.Comp.StrainInflection, ent.Comp.StrainSteepness);
        ent.Comp.Strain = WhiteMath.Diff(strain - ent.Comp.Strain, (float) _gameTiming.FrameTime.TotalSeconds);

        UpdateHeartRate(ent);
    }

    #endregion
}
