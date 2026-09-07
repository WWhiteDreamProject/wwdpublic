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
    }

    #region Event Handling

    private void OnGotInserted(Entity<RespiratorConsumerComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        UpdateConsumption(args.Body.Owner);
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

    #endregion

    #region Private API

    private void UpdateConsumer(Entity<RespiratorConsumerComponent> ent)
    {
        ent.Comp.NextUpdate += ent.Comp.UpdateInterval;

        if (ent.Comp.Threshold < ent.Comp.BloodLevel * ent.Comp.SaturationLevel * ent.Comp.MetabolicRate)
        {
            _damageable.ChangeDamage(ent.Owner, -ent.Comp.Damage);
            return;
        }

        _damageable.ChangeDamage(ent.Owner, ent.Comp.Damage);
    }

    #endregion
}
