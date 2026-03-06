using Content.Server._White.Body.Respirator.Components;
using Content.Shared._White.Body.Bloodstream.Systems;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Systems;

namespace Content.Server._White.Body.Respirator.Systems;

public sealed partial class RespiratorSystem
{
    private void InitializeConsumer()
    {
        SubscribeLocalEvent<RespiratorСonsumerComponent, OrganRelayedEvent<AfterBloodAmountChangedEvent>>(OnAfterBloodLevelChanged);
        SubscribeLocalEvent<RespiratorСonsumerComponent, OrganRelayedEvent<AfterSaturationLevelChangedEvent>>(OnAfterSaturationLevelChanged);
        SubscribeLocalEvent<RespiratorСonsumerComponent, OrganRelayedEvent<ApplyMetabolicRateEvent>>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<RespiratorСonsumerComponent, OrganRelayedEvent<BeforeUpdateRespiratorEvent>>(OnBeforeUpdateRespirator);
    }

    #region Event Handling

    private void OnAfterBloodLevelChanged(Entity<RespiratorСonsumerComponent> ent, ref OrganRelayedEvent<AfterBloodAmountChangedEvent> args) =>
        ent.Comp.BloodLevel = (args.Args.BloodAmount / args.Args.Bloodstream.Comp.CurrentBloodMaxVolume).Float();

    private void OnAfterSaturationLevelChanged(Entity<RespiratorСonsumerComponent> ent, ref OrganRelayedEvent<AfterSaturationLevelChangedEvent> args) =>
        ent.Comp.SaturationLevel = args.Args.SaturationLevel;

    private void OnApplyMetabolicMultiplier(Entity<RespiratorСonsumerComponent> ent, ref OrganRelayedEvent<ApplyMetabolicRateEvent> args) =>
        ent.Comp.MetabolicRate = args.Args.Rate;

    private void OnBeforeUpdateRespirator(Entity<RespiratorСonsumerComponent> ent, ref OrganRelayedEvent<BeforeUpdateRespiratorEvent> args) =>
        args.Args = new (SaturationConsumption: args.Args.SaturationConsumption + ent.Comp.SaturationConsumption);

    private void UpdateConsumers()
    {
        var query = EntityQueryEnumerator<RespiratorСonsumerComponent>();
        while (query.MoveNext(out var uid, out var consumer))
        {
            if (_gameTiming.CurTime < consumer.NextUpdate)
                continue;

            consumer.NextUpdate += consumer.UpdateInterval;

            UpdateConsumer((uid, consumer));
        }
    }

    #endregion

    #region Private API

    private void UpdateConsumer(Entity<RespiratorСonsumerComponent> ent)
    {
        var oxygenateLevel = ent.Comp.BloodLevel * ent.Comp.SaturationLevel * ent.Comp.MetabolicRate;

        if (ent.Comp.DamageThreshold < oxygenateLevel)
            return;

        _wound.ChangeOrganDamage(ent.Owner, ent.Comp.Damage);
    }

    #endregion
}
