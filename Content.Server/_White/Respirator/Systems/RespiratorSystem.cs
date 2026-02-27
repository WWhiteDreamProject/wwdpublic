using Content.Server._White.Bloodstream.Systems;
using Content.Server._White.Respirator.Components;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Prototypes;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._White.Respirator.Systems;

[UsedImplicitly]
public sealed partial class RespiratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly WoundableSystem _woundable = default!;

    private EntityQuery<RespiratorComponent> _respiratorQuery;

    private static readonly ProtoId<MetabolismStagePrototype> RespirationId = new("Respiration");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RespiratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RespiratorComponent, MetabolicRateChangedEvent>(OnMetabolicRateChanged);

        InitializeConsumer();
        InitializeProvider();

        _respiratorQuery = GetEntityQuery<RespiratorComponent>();
    }

    #region Event Handling

    private void OnMapInit(Entity<RespiratorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.CurrentUpdateInterval;
    }

    private void OnMetabolicRateChanged(Entity<RespiratorComponent> ent, ref MetabolicRateChangedEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Rate == 0 ? 0 : 1 / args.Rate;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var respiratorQuery = EntityQueryEnumerator<RespiratorComponent>();
        while (respiratorQuery.MoveNext(out var uid, out var respirator))
        {
            if (respirator.CurrentUpdateInterval == TimeSpan.Zero || _gameTiming.CurTime < respirator.NextUpdate)
                continue;

            UpdateRespirator((uid, respirator));
        }

        var consumerQuery = EntityQueryEnumerator<RespiratorСonsumerComponent>();
        while (consumerQuery.MoveNext(out var uid, out var consumer))
        {
            if (_gameTiming.CurTime < consumer.NextUpdate)
                continue;

            UpdateConsumer((uid, consumer));
        }
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
        if (!_respiratorQuery.Resolve(ent, ref ent.Comp))
            return false;

        var getInhaleLocationEv = new GetInhaleLocationEvent(ent.Comp);
        RaiseLocalEvent(ent, ref getInhaleLocationEv);

        getInhaleLocationEv.Gas ??= _atmosphere.GetContainingMixture(ent.Owner, excite: true);

        return getInhaleLocationEv.Gas is not null && CanMetabolizeInhaledAir(ent, getInhaleLocationEv.Gas);
    }

    /// <summary>
    /// Checks if a given entity can safely metabolize a given gas mixture.
    /// </summary>
    /// <param name="ent">The entity attempting to metabolize the gas.</param>
    /// <param name="gas">The gas mixture we are trying to metabolize.</param>
    /// <returns>Returns true only if the gas mixture is not toxic, and it wouldn't suffocate.</returns>
    public bool CanMetabolizeInhaledAir(Entity<RespiratorComponent?> ent, GasMixture gas)
    {
        if (!_respiratorQuery.Resolve(ent, ref ent.Comp))
            return false;

        var ev = new CanMetabolizeGasEvent(gas);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Toxic)
            return false;

        return ev.Saturation > ent.Comp.Consumption;
    }

    /// <summary>
    /// Changes the saturation level of the entity.
    /// </summary>
    /// <param name="ent">The entity subject to saturation level change.</param>
    /// <param name="amount">The amount by which to change the saturation level.</param>
    public void ChangeSaturationLevel(Entity<RespiratorComponent?> ent, float amount)
    {
        if (!_respiratorQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var oldLevel = ent.Comp.Level;
        ent.Comp.Level = Math.Clamp(ent.Comp.Level + amount, 0, 1);

        var ev = new SaturationLevelChangedEvent(ent.Comp.Level - oldLevel);
        RaiseLocalEvent(ent, ev);
    }

    /// <summary>
    /// Updates the saturation consumption of the entity.
    /// </summary>
    /// <param name="ent">The entity subject to saturation consumption change.</param>
    public void UpdateConsumption(Entity<RespiratorComponent?> ent)
    {
        if (!_respiratorQuery.Resolve(ent, ref ent.Comp))
            return;

        var ev = new GetSaturationConsumption();
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.Consumption = ev.Consumption;
    }

    /// <summary>
    /// Updates the breath volume of the entity.
    /// </summary>
    /// <param name="ent">The entity subject to breath volume change.</param>
    public void UpdateVolume(Entity<RespiratorComponent?> ent)
    {
        if (!_respiratorQuery.Resolve(ent, ref ent.Comp))
            return;

        var ev = new GetBreathVolumeEvent();
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.Volume = ev.Volume;
    }

    #endregion

    #region Private API

    private void Exhale(Entity<RespiratorComponent> ent)
    {
        var getExhaleLocationEv = new GetExhaleLocationEvent();
        RaiseLocalEvent(ent, ref getExhaleLocationEv, broadcast: false);

        if (getExhaleLocationEv.Gas is not {} gas)
        {
            gas = _atmosphere.GetContainingMixture(ent.Owner, excite: true);

            // Walls and grids without atmos comp return null. I guess it makes sense to not be able to exhale in walls,
            // but this also means you cannot exhale on some grids.
            gas ??= GasMixture.SpaceGas;
        }

        var exhaleEv = new ExhaleEvent(gas);
        RaiseLocalEvent(ent, ref exhaleEv);
    }

    private void Inhale(Entity<RespiratorComponent> ent)
    {
        var getInhaleLocationEv = new GetInhaleLocationEvent(ent.Comp);
        RaiseLocalEvent(ent, ref getInhaleLocationEv);

        getInhaleLocationEv.Gas ??= _atmosphere.GetContainingMixture(ent.Owner, excite: true);

        if (getInhaleLocationEv.Gas is null)
            return;

        var gas = getInhaleLocationEv.Gas.RemoveVolume(ent.Comp.Volume);

        var inhaleEv = new InhaleEvent(gas);
        RaiseLocalEvent(ent, ref inhaleEv);

        if (inhaleEv.Succeeded)
            return;

        // If nothing could inhale, the gasses give it back.
        _atmosphere.Merge(getInhaleLocationEv.Gas, gas);
    }

    private void UpdateRespirator(Entity<RespiratorComponent> ent)
    {
        ent.Comp.NextUpdate += ent.Comp.CurrentUpdateInterval;

        if (_mobState.IsDead(ent))
            return;

        ChangeSaturationLevel(ent.AsNullable(), -ent.Comp.Consumption);

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

        if (ent.Comp.Level < ent.Comp.SuffocationThreshold)
        {
            if (_gameTiming.CurTime >= ent.Comp.LastGaspTime + ent.Comp.GaspCooldown)
            {
                ent.Comp.LastGaspTime = _gameTiming.CurTime;
                _chat.TryEmoteWithChat(ent, ent.Comp.Gasp, ChatTransmitRange.HideChat, ignoreActionBlocker: true);
            }

            if (ent.Comp.Suffocation == 2)
                _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} started suffocating");

            if (ent.Comp.Suffocation < ent.Comp.SuffocationThreshold)
                return;

            ent.Comp.Suffocation += 1;
            RaiseLocalEvent(ent, new SuffocationChangedEvent(ent.Comp.Suffocation));

            return;
        }

        if (ent.Comp.Suffocation == 0)
            return;

        if (ent.Comp.Suffocation == 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} started suffocating");

        if (ent.Comp.Suffocation < ent.Comp.SuffocationThreshold)
            return;

        ent.Comp.Suffocation = 0;
        RaiseLocalEvent(ent, new SuffocationChangedEvent(ent.Comp.Suffocation));
    }

    #endregion
}

/// <summary>
/// An event raised to inhalation handlers that asks them nicely to simulate what it would be like to metabolize
/// a given volume of gas, without actually metabolizing it.
/// </summary>
/// <param name="Gas">The gas mixture we are testing.</param>
/// <param name="Toxic">Whether the gas returns as toxic to any respirator.</param>
/// <param name="Saturation">The amount of saturation we got from the gas.</param>
[ByRefEvent]
public record struct CanMetabolizeGasEvent(GasMixture Gas, bool Toxic = false, float Saturation = 0f) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised when an entity is performing an exhalation action.
/// </summary>
/// <param name="Gas">The gas mixture being exhaled into the environment.</param>
[ByRefEvent]
public record struct ExhaleEvent(GasMixture Gas) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised to determine the volume of gas an entity will attempt to inhale in a single breath.
/// </summary>
/// <param name="Volume">The amount of volume to breathe in.</param>
[ByRefEvent]
public record struct GetBreathVolumeEvent(float Volume = 0f) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised when an entity first tries to exhale a gas, determines where the gas they're exhaling will be sent.
/// </summary>
/// <param name="Gas">The gas mixture that the exhaled gas will be merged into.</param>
[ByRefEvent]
public record struct GetExhaleLocationEvent(GasMixture? Gas);

/// <summary>
/// Event raised when an entity first tries to inhale that returns a GasMixture from a given location.
/// </summary>
/// <param name="Respirator">The Respirator component of the entity attempting to inhale</param>
/// <param name="Gas">The gas, which gets returned, null if there is none.</param>
[ByRefEvent]
public record struct GetInhaleLocationEvent(RespiratorComponent Respirator, GasMixture? Gas = null);

/// <summary>
/// Event raised to determine the saturation consumption of an entity.
/// </summary>
/// <param name="Consumption">The amount of saturation consumption.</param>
[ByRefEvent]
public record struct GetSaturationConsumption(float Consumption = 0f) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised when an entity successfully inhales a gas, attempts to find a place to put the gas.
/// </summary>
/// <param name="Gas">The gas we're inhaling.</param>
/// <param name="Succeeded">Whether we successfully managed to inhale the gas</param>
[ByRefEvent]
public record struct InhaleEvent(GasMixture Gas, bool Succeeded = false) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised after an entity's oxidant saturation level has changed.
/// </summary>
public record struct SaturationLevelChangedEvent(float Level) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised after an entity's suffocation has changed.
/// </summary>
public record struct SuffocationChangedEvent(int Suffocation) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}
