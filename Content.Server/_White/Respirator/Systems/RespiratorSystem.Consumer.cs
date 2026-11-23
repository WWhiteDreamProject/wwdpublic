using Content.Server._White.Respirator.Components;
using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body.Systems;

namespace Content.Server._White.Respirator.Systems;

public sealed partial class RespiratorSystem
{
    private void InitializeConsumer()
    {
        SubscribeLocalEvent<RespiratorConsumerComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<RespiratorConsumerComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<RespiratorConsumerComponent, BodyRelayedEvent<BloodAmountChangedEvent>>(OnBloodAmountChanged);
        SubscribeLocalEvent<RespiratorConsumerComponent, BodyRelayedEvent<GetSaturationConsumption>>(OnGetSaturationConsumption);
        SubscribeLocalEvent<RespiratorConsumerComponent, BodyRelayedEvent<MetabolicRateChangedEvent>>(OnMetabolicRateChanged);
        SubscribeLocalEvent<RespiratorConsumerComponent, BodyRelayedEvent<SaturationLevelChangedEvent>>(OnSaturationLevelChanged);
        SubscribeLocalEvent<RespiratorConsumerComponent, MapInitEvent>(OnMapInit);
    }

    #region Event Handling

    private void OnGotInserted(Entity<RespiratorConsumerComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        var body = args.Body.Owner;
        if (_bloodstreamQuery.TryComp(body, out var bloodstream))
        {
            ent.Comp.BloodLevel = _bloodstream.GetBloodLevel((body, bloodstream));
            ent.Comp.MetabolicRate = bloodstream.MetabolicRate;
        }

        if (_respiratorQuery.TryComp(body, out var respirator))
            ent.Comp.SaturationLevel = respirator.Level;

        UpdateConsumption(body);
    }

    private void OnGotRemoved(Entity<RespiratorConsumerComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        UpdateConsumption(args.Body.Owner);
    }

    private void OnBloodAmountChanged(Entity<RespiratorConsumerComponent> ent, ref BodyRelayedEvent<BloodAmountChangedEvent> args)
    {
        ent.Comp.BloodLevel = args.Args.Level;
    }

    private void OnGetSaturationConsumption(Entity<RespiratorConsumerComponent> ent, ref BodyRelayedEvent<GetSaturationConsumption> args)
    {
        args.Args = new(args.Args.Consumption + ent.Comp.Consumption);
    }

    private void OnMetabolicRateChanged(Entity<RespiratorConsumerComponent> ent, ref BodyRelayedEvent<MetabolicRateChangedEvent> args)
    {
        ent.Comp.MetabolicRate = args.Args.Rate;
    }

    private void OnSaturationLevelChanged(Entity<RespiratorConsumerComponent> ent, ref BodyRelayedEvent<SaturationLevelChangedEvent> args)
    {
        ent.Comp.SaturationLevel = args.Args.Level;
    }

    private void OnMapInit(Entity<RespiratorConsumerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + GetConsumerUpdateInterval(ent);
    }

    #endregion

    #region Private API

    /// <summary>
    /// A slower metabolism (e.g. stasis) slows down both the damage and the recovery of the consumer.
    /// Without circulation at all, the consumer keeps suffocating at the base rate.
    /// </summary>
    private static TimeSpan GetConsumerUpdateInterval(Entity<RespiratorConsumerComponent> ent)
    {
        return ent.Comp.MetabolicRate > 0f ? ent.Comp.UpdateInterval / ent.Comp.MetabolicRate : ent.Comp.UpdateInterval;
    }

    private void UpdateConsumer(Entity<RespiratorConsumerComponent> ent)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + GetConsumerUpdateInterval(ent);

        var oxygenated = ent.Comp.MetabolicRate > 0f && ent.Comp.BloodLevel * ent.Comp.SaturationLevel > ent.Comp.Threshold;
        _damageable.ChangeDamage(ent.Owner, oxygenated ? -ent.Comp.Damage : ent.Comp.Damage);
    }

    #endregion
}
