using Content.Server._White.Body.Respirator.Components;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Database;

namespace Content.Server._White.Body.Respirator.Systems;

public sealed partial class RespiratorSystem
{
    private void InitializeRespirator()
    {
        SubscribeLocalEvent<RespiratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RespiratorComponent, ApplyMetabolicRateEvent>(OnApplyMetabolicMultiplier);
    }

    #region Event Handling

    private void OnMapInit(Entity<RespiratorComponent> ent, ref MapInitEvent args) =>
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.AdjustedUpdateInterval;

    private void OnApplyMetabolicMultiplier(Entity<RespiratorComponent> ent, ref ApplyMetabolicRateEvent args) =>
        ent.Comp.UpdateIntervalMultiplier = args.Rate == 0 ? 0 : 1 / args.Rate;

    private void UpdateRespirators()
    {
        var query = EntityQueryEnumerator<RespiratorComponent>();
        while (query.MoveNext(out var uid, out var respirator))
        {
            if (respirator.AdjustedUpdateInterval == TimeSpan.Zero || _gameTiming.CurTime < respirator.NextUpdate)
                continue;

            respirator.NextUpdate += respirator.AdjustedUpdateInterval;

            UpdateRespirator((uid, respirator));
        }
    }

    #endregion

    #region Private API

    private void Exhale(Entity<RespiratorComponent> ent)
    {
        var ev = new ExhaleLocationEvent();
        RaiseLocalEvent(ent, ref ev, broadcast: false);

        if (ev.Gas is null)
        {
            ev.Gas = _atmosphere.GetContainingMixture(ent.Owner, excite: true);

            // Walls and grids without atmos comp return null. I guess it makes sense to not be able to exhale in walls,
            // but this also means you cannot exhale on some grids.
            ev.Gas ??= GasMixture.SpaceGas;
        }

        var exhaleEv = new ExhaledGasEvent(ev.Gas);
        RaiseLocalEvent(ent, ref exhaleEv);
    }

    private void Inhale(Entity<RespiratorComponent> ent)
    {
        var ev = new InhaleLocationEvent
        {
            Respirator = ent.Comp
        };
        RaiseLocalEvent(ent, ref ev);

        ev.Gas ??= _atmosphere.GetContainingMixture(ent.Owner, excite: true);

        if (ev.Gas is null)
            return;

        var breathEv = new BeforeBreathEvent();
        RaiseLocalEvent(ent, ref breathEv);

        var gas = ev.Gas.RemoveVolume(breathEv.BreathVolume);

        var inhaleEv = new InhaledGasEvent(gas);
        RaiseLocalEvent(ent, ref inhaleEv);

        if (inhaleEv is { Succeeded: true, })
            return;

        // If nothing could inhale, the gasses give it back.
        _atmosphere.Merge(ev.Gas, gas);
    }

    private void UpdateRespirator(Entity<RespiratorComponent> ent)
    {
        if (_mobState.IsDead(ent))
            return;

        var ev = new BeforeUpdateRespiratorEvent();
        RaiseLocalEvent(ent, ref ev);

        ChangeSaturationLevel(ent.AsNullable(), -ev.SaturationConsumption);

        if (!_mobState.IsIncapacitated(ent))
        {
            switch (ent.Comp.Status)
            {
                case RespiratorStatus.Inhaling:
                    Inhale(ent);
                    ent.Comp.Status = RespiratorStatus.Exhaling;
                    break;
                case RespiratorStatus.Exhaling:
                    Exhale(ent);
                    ent.Comp.Status = RespiratorStatus.Inhaling;
                    break;
            }
        }

        if (ent.Comp.SaturationLevel < ent.Comp.SuffocationThreshold)
        {
            if (_gameTiming.CurTime >= ent.Comp.LastGaspEmoteTime + ent.Comp.GaspEmoteCooldown)
            {
                ent.Comp.LastGaspEmoteTime = _gameTiming.CurTime;
                _chat.TryEmoteWithChat(ent, ent.Comp.GaspEmote, ChatTransmitRange.HideChat, ignoreActionBlocker: true);
            }

            StartSuffocation(ent);
            ent.Comp.SuffocationCycles += 1;
            return;
        }

        StopSuffocation(ent);
        ent.Comp.SuffocationCycles = 0;
    }

    private void StartSuffocation(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles == 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} started suffocating");

        if (ent.Comp.SuffocationCycles < ent.Comp.SuffocationCycleThreshold)
            return;

        var ev = new SuffocationEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void StopSuffocation(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles >= 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} stopped suffocating");

        var ev = new StopSuffocatingEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Checks if it's safe for a given entity to breathe the air from the environment it is currently situated in.
    /// </summary>
    /// <param name="ent">The entity attempting to metabolize the gas.</param>
    /// <returns>Returns true only if the air is not toxic, and it wouldn't suffocate.</returns>
    public bool CanMetabolizeInhaledAir(Entity<RespiratorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Get the gas at our location but don't actually remove it from the gas mixture.
        var ev = new InhaleLocationEvent
        {
            Respirator = ent.Comp,
        };
        RaiseLocalEvent(ent, ref ev);

        ev.Gas ??= _atmosphere.GetContainingMixture(ent.Owner, excite: true);

        // If there's no air to breathe or we can't metabolize it then internals should be on.
        return ev.Gas is not null && CanMetabolizeInhaledAir(ent, ev.Gas);
    }

    /// <summary>
    /// Checks if a given entity can safely metabolize a given gas mixture.
    /// </summary>
    /// <param name="ent">The entity attempting to metabolize the gas.</param>
    /// <param name="gas">The gas mixture we are trying to metabolize.</param>
    /// <returns>Returns true only if the gas mixture is not toxic, and it wouldn't suffocate.</returns>
    public bool CanMetabolizeInhaledAir(Entity<RespiratorComponent?> ent, GasMixture gas)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var ev = new CanMetabolizeGasEvent(gas);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Toxic)
            return false;

        var beforeUpdateEv = new BeforeUpdateRespiratorEvent();
        RaiseLocalEvent(ent, ref beforeUpdateEv);

        return ev.Saturation > beforeUpdateEv.SaturationConsumption;
    }

    public void ChangeSaturationLevel(Entity<RespiratorComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var oldSaturationLevel = ent.Comp.SaturationLevel;
        ent.Comp.SaturationLevel = Math.Clamp(ent.Comp.SaturationLevel + amount, 0, 1);

        var ev = new AfterSaturationLevelChangedEvent(ent.Comp.SaturationLevel, oldSaturationLevel);
        RaiseLocalEvent(ent, ev);
    }

    #endregion
}
