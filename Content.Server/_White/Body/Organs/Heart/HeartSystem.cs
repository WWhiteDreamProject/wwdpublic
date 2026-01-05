using Content.Server._White.Body.Organs.Metabolizer;
using Content.Server._White.Body.Respirator.Systems;
using Content.Server._White.Body.Wound.Systems;
using Content.Shared._White.Body.Bloodstream.Systems;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Body.Organs.Heart;

public sealed class HeartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HeartComponent, AfterOrganToggledEvent>(OnAfterOrganToggled);
        SubscribeLocalEvent<HeartComponent, OrganRelayedEvent<AfterBloodAmountChangedEvent>>(OnAfterBloodLevelChanged);
        SubscribeLocalEvent<HeartComponent, OrganRelayedEvent<AfterSaturationLevelChangedEvent>>(OnAfterSaturationLevelChanged);
        SubscribeLocalEvent<HeartComponent, OrganRelayedEvent<ApplyMetabolicRateEvent>>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<HeartComponent, OrganRelayedEvent<GetMetabolicRateEvent>>(OnGetMetabolicMultiplier);
        SubscribeLocalEvent<HeartComponent, OrganAddedEvent>(OnOrganAdded);
        SubscribeLocalEvent<HeartComponent, OrganHealthChangedEvent>(OnOrganHealthChanged);
        SubscribeLocalEvent<HeartComponent, OrganRemovedEvent>(OnOrganRemoved);
    }

    private void OnAfterOrganToggled(Entity<HeartComponent> ent, ref AfterOrganToggledEvent args)
    {
        ent.Comp.Enable = args.Enable;
        UpdateStrain(ent);
    }

    private void OnAfterBloodLevelChanged(Entity<HeartComponent> ent, ref OrganRelayedEvent<AfterBloodAmountChangedEvent> args)
    {
        ent.Comp.BloodLevel = (args.Args.BloodAmount / args.Args.Bloodstream.Comp.CurrentBloodMaxVolume).Float();
        UpdateStrain(ent);
    }

    private void OnAfterSaturationLevelChanged(Entity<HeartComponent> ent, ref OrganRelayedEvent<AfterSaturationLevelChangedEvent> args)
    {
        ent.Comp.SaturationLevel = args.Args.SaturationLevel;
        UpdateStrain(ent);
    }

    private void OnApplyMetabolicMultiplier(Entity<HeartComponent> ent, ref OrganRelayedEvent<ApplyMetabolicRateEvent> args)
    {
        ent.Comp.MetabolicRate = args.Args.Rate;
        UpdateStrain(ent);
    }

    private void OnGetMetabolicMultiplier(Entity<HeartComponent> ent, ref OrganRelayedEvent<GetMetabolicRateEvent> args) =>
        args.Args = new(args.Args.Rate + (!ent.Comp.Enable ? 0f : 1 + ent.Comp.Strain));

    private void OnOrganAdded(Entity<HeartComponent> ent, ref OrganAddedEvent args)
    {
        if (!args.Body.HasValue)
            return;

        _metabolizer.UpdateMetabolicRate(args.Body.Value);
    }

    private void OnOrganHealthChanged(Entity<HeartComponent> ent, ref OrganHealthChangedEvent args)
    {
        ent.Comp.HealthFactor = (args.Organ.Comp2.Health / args.Organ.Comp2.MaximumHealth).Float();
        UpdateStrain(ent);

        if (args.Organ.Comp1.Body.HasValue)
            _metabolizer.UpdateMetabolicRate(args.Organ.Comp1.Body.Value);
    }

    private void OnOrganRemoved(Entity<HeartComponent> ent, ref OrganRemovedEvent args)
    {
        if (!args.Body.HasValue)
            return;

        _metabolizer.UpdateMetabolicRate(args.Body.Value);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeartComponent>();
        while (query.MoveNext(out var uid, out var heart))
        {
            if (!heart.Enable || _gameTiming.CurTime < heart.NextUpdate)
                continue;

            heart.NextUpdate += TimeSpan.FromSeconds(60 / heart.HeartRate);
            UpdateHeartRate((uid, heart));

            if (_random.Prob(heart.CurrentStrainDamageChanceThreshold))
                _wound.ChangeOrganDamage(uid, heart.StrainDamage);
        }
    }

    private void UpdateHeartRate(Entity<HeartComponent> ent)
    {
        if (!ent.Comp.Enable)
        {
            ent.Comp.HeartRate = 0;
            return;
        }

        var deviation = _random.Next(-ent.Comp.HeartRateDeviation, ent.Comp.HeartRateDeviation);
        ent.Comp.HeartRate = Math.Max((int)MathHelper.Lerp(ent.Comp.MinHeartRate, ent.Comp.MaxHeartRate, ent.Comp.Strain) + deviation, 0);
    }

    private void UpdateStrain(Entity<HeartComponent> ent)
    {
        var invert = 0f;
        if (ent.Comp.OxygenSupply != 0 && ent.Comp.MetabolicRate != 0)
            invert = MathF.Log(ent.Comp.MetabolicRate / ent.Comp.OxygenSupply);

        if (!float.IsFinite(invert))
            throw new InvalidOperationException($"demand/supply {ent.Comp.MetabolicRate}/{ent.Comp.OxygenSupply} is not finite: {invert}");

        var healthFactor = !ent.Comp.Enable ? 0f : 1f - ent.Comp.HealthFactor;

        ent.Comp.Strain = Math.Clamp(healthFactor + invert, 0f, 1f);
        ent.Comp.CurrentStrainDamageChanceThreshold = ent.Comp.StrainDamageChanceThresholds.HighestMatch(ent.Comp.Strain) ?? 0f;

        UpdateHeartRate(ent);
    }
}
