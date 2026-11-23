using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Pain.Components;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._White.Pain.Systems;

public abstract partial class SharedPainfulSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private EntityQuery<PainfulComponent> _painfulQuery;
    private EntityQuery<PainfulProviderComponent> _providerQuery;
    private EntityQuery<PainfulWoundComponent> _woundQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainfulComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PainfulComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<PainfulComponent, MapInitEvent>(OnMapInit);

        InitializeProvider();
        InitializeStatus();
        InitializeThresholds();
        InitializeWound();

        _painfulQuery = GetEntityQuery<PainfulComponent>();
        _providerQuery = GetEntityQuery<PainfulProviderComponent>();
        _woundQuery = GetEntityQuery<PainfulWoundComponent>();
    }

    #region Event Handling

    private void OnGetState(Entity<PainfulComponent> ent, ref ComponentGetState args)
    {
        args.State = new PainfulComponentState(ent.Comp);
    }

    private void OnHandleState(Entity<PainfulComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not PainfulComponentState state)
            return;

        ent.Comp.PainMultiplier = state.PainMultiplier;
        ent.Comp.UpdateIntervalMultiplier = state.UpdateIntervalMultiplier;

        var delta = state.Pain - ent.Comp.Pain;

        if (delta == FixedPoint2.Zero)
            return;

        ent.Comp.Pain = state.Pain;

        var ev = new PainChangedEvent(ent, delta);
        RaiseLocalEvent(ent, ref ev, true);
    }

    private void OnMapInit(Entity<PainfulComponent> ent, ref MapInitEvent args)
    {
        UpdatePain((ent, ent.Comp));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = GameTiming.CurTime;
        var query = EntityQueryEnumerator<PainfulComponent>();
        while (query.MoveNext(out var uid, out var painful))
        {
            if (painful.NextUpdate is not { } nextUpdate || curTime < nextUpdate)
                continue;

            UpdatePain((uid, painful));
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Changes the entity's pain by a relative amount.
    /// </summary>
    /// <param name="ent">The entity whose pain should be modified.</param>
    /// <param name="pain">The amount to add to the current pain (can be negative to reduce pain).</param>
    public void ChangePain(Entity<PainfulComponent?> ent, FixedPoint2 pain)
    {
        if (!_painfulQuery.Resolve(ent, ref ent.Comp))
            return;

        if (pain == FixedPoint2.Zero)
            return;

        ent.Comp.Pain = FixedPoint2.Max(0, ent.Comp.Pain + pain);
        Dirty(ent);

        var ev = new PainChangedEvent((ent, ent.Comp), pain);
        RaiseLocalEvent(ent, ref ev, true);
    }

    /// <summary>
    /// Sets the entity's pain to an value.
    /// Internally calculates the delta and calls <see cref="ChangePain"/>.
    /// </summary>
    /// <param name="ent">The entity whose pain should be set.</param>
    /// <param name="pain">The pain value to set.</param>
    public void SetPain(Entity<PainfulComponent?> ent, FixedPoint2 pain)
    {
        if (!_painfulQuery.Resolve(ent, ref ent.Comp))
            return;

        ChangePain(ent, pain - ent.Comp.Pain);
    }

    /// <summary>
    /// Retrieves the current pain value of the entity.
    /// </summary>
    /// <param name="ent">The entity to get pain from.</param>
    /// <returns>The entity's current pain value.</returns>
    public FixedPoint2 GetPain(Entity<PainfulComponent?> ent)
    {
        if (!_painfulQuery.Resolve(ent, ref ent.Comp))
            return FixedPoint2.Zero;

        return ent.Comp.CurrentPain;
    }

    /// <summary>
    /// Recollects pain from all sources of the entity.
    /// Called when the sources change; while some pain is fading, it is also called every update interval.
    /// </summary>
    public void UpdatePain(Entity<PainfulComponent?> ent)
    {
        if (GameTiming.ApplyingState)
            return;

        if (!_painfulQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new GetPainEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.NextUpdate = ev.Decay > 0f ? GameTiming.CurTime + ent.Comp.CurrentUpdateInterval : null;

        SetPain(ent, ev.Pain);
    }

    #endregion
}

/// <summary>
/// Event raised on an entity to query its current total pain value.
/// </summary>
/// <param name="Pain">The accumulated pain value from all relevant sources. Initialized to zero and populated by providers.</param>
/// <param name="Decay">How fast the collected pain fades, per second. Zero if nothing fades.</param>
[ByRefEvent]
public record struct GetPainEvent(FixedPoint2 Pain, float Decay = 0f) : IBodyRelayEvent, IWoundRelayEvent
{
    BodyProviderType IBodyRelayEvent.Type => BodyProviderType.All;
    ProtoId<DamageTypePrototype>? IWoundRelayEvent.Type => null;

    public readonly GetPainEvent Add(FixedPoint2 pain, float decay = 0f)
    {
        return new GetPainEvent(Pain + pain, Decay + decay);
    }

    public readonly GetPainEvent Add(GetPainEvent other)
    {
        return Add(other.Pain, other.Decay);
    }
}

/// <summary>
/// Event raised on an entity after its total pain value has been changed.
/// </summary>
/// <param name="Painful">This is the entity whose pain was changed.</param>
/// <param name="Pain">The amount by which the pain has changed.</param>
[ByRefEvent]
public record struct PainChangedEvent(Entity<PainfulComponent> Painful, FixedPoint2 Pain) : IBodyRelayEvent
{
    BodyProviderType IBodyRelayEvent.Type => BodyProviderType.All;
}

/// <summary>
/// Event raised on an entity after its pain level has been changed.
/// </summary>
/// <param name="Level">The new pain level.</param>
/// <param name="Location">The specific body location when pain level changed.</param>
[ByRefEvent]
public record struct PainLevelChangedEvent(PainLevel Level, BodyProviderType Location = BodyProviderType.All);
