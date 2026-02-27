using Content.Server._White.Respirator.Components;
using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body.Systems;

namespace Content.Server._White.Respirator.Systems;

public sealed partial class RespiratorSystem
{
    private void InitializeConsumer()
    {
        SubscribeLocalEvent<RespiratorСonsumerComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<RespiratorСonsumerComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<RespiratorСonsumerComponent, BodyRelayedEvent<BloodAmountChangedEvent>>(OnBloodAmountChanged);
        SubscribeLocalEvent<RespiratorСonsumerComponent, BodyRelayedEvent<GetSaturationConsumption>>(OnGetSaturationConsumption);
        SubscribeLocalEvent<RespiratorСonsumerComponent, BodyRelayedEvent<MetabolicRateChangedEvent>>(OnMetabolicRateChanged);
        SubscribeLocalEvent<RespiratorСonsumerComponent, BodyRelayedEvent<SaturationLevelChangedEvent>>(OnSaturationLevelChanged);
    }

    #region Event Handling

    private void OnGotInserted(Entity<RespiratorСonsumerComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        UpdateConsumption(args.Body);
    }

    private void OnGotRemoved(Entity<RespiratorСonsumerComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        UpdateConsumption(args.Body);
    }

    private void OnBloodAmountChanged(Entity<RespiratorСonsumerComponent> ent, ref BodyRelayedEvent<BloodAmountChangedEvent> args)
    {
        ent.Comp.BloodLevel = args.Args.Level;
    }

    private void OnGetSaturationConsumption(Entity<RespiratorСonsumerComponent> ent, ref BodyRelayedEvent<GetSaturationConsumption> args)
    {
        args.Args = new(args.Args.Consumption + ent.Comp.Consumption);
    }

    private void OnMetabolicRateChanged(Entity<RespiratorСonsumerComponent> ent, ref BodyRelayedEvent<MetabolicRateChangedEvent> args)
    {
        ent.Comp.MetabolicRate = args.Args.Rate;
    }

    private void OnSaturationLevelChanged(Entity<RespiratorСonsumerComponent> ent, ref BodyRelayedEvent<SaturationLevelChangedEvent> args)
    {
        ent.Comp.SaturationLevel = args.Args.Level;
    }

    #endregion

    #region Private API

    private void UpdateConsumer(Entity<RespiratorСonsumerComponent> ent)
    {
        ent.Comp.NextUpdate += ent.Comp.UpdateInterval;

        if (ent.Comp.DamageThreshold < ent.Comp.BloodLevel * ent.Comp.SaturationLevel * ent.Comp.MetabolicRate)
            return;

        _woundable.ChangeDamage(ent.Owner, ent.Comp.Damage);
    }

    #endregion
}
