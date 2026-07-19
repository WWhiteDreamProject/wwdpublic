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
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private EntityQuery<PainfulComponent> _painfulQuery;
    private EntityQuery<PainfulProviderComponent> _providerQuery;

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
        ent.Comp.LastUpdate = state.LastUpdate;

        var painDelta = state.Pain - ent.Comp.Pain;

        if (painDelta == FixedPoint2.Zero)
            return;

        ent.Comp.Pain = state.Pain;

        RaiseLocalEvent(ent, new PainChangedEvent(ent, painDelta), true);
    }

    private void OnMapInit(Entity<PainfulComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastUpdate = _gameTiming.CurTime;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PainfulComponent>();
        while (query.MoveNext(out var uid, out var painful))
        {
            if (painful.LastUpdate + painful.CurrentUpdateInterval >= _gameTiming.CurTime)
                continue;

            UpdatePain((uid, painful));
        }
    }

    #endregion

    #region Public API

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

    #endregion

    #region Private API

    private void UpdatePain(Entity<PainfulComponent> ent)
    {
        var timeDelta = _gameTiming.CurTime - ent.Comp.LastUpdate;
        ent.Comp.LastUpdate = _gameTiming.CurTime;
        Dirty(ent);

        var getPainEv = new GetPainEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref getPainEv);

        if (getPainEv.Pain == ent.Comp.Pain)
            return;

        var painDelta = FixedPoint2.Zero;

        if (ent.Comp.Pain < getPainEv.Pain)
        {
            var maxIncrease = timeDelta.TotalSeconds * ent.Comp.MaxPainIncreasePerSecond;
            painDelta = FixedPoint2.Min(getPainEv.Pain - ent.Comp.Pain, maxIncrease);
        }

        if (ent.Comp.Pain > getPainEv.Pain)
        {
            var maxDecrease = timeDelta.TotalSeconds * ent.Comp.MaxPainDecreasePerSecond;
            painDelta = -FixedPoint2.Min(ent.Comp.Pain - getPainEv.Pain, maxDecrease);
        }

        if (ent.Comp.Pain + painDelta < FixedPoint2.Zero)
            painDelta = -ent.Comp.Pain;

        if (painDelta == FixedPoint2.Zero)
            return;

        ent.Comp.Pain += painDelta;
        Dirty(ent);

        RaiseLocalEvent(ent, new PainChangedEvent(ent, painDelta), true);
    }

    #endregion
}

/// <summary>
/// Event raised on an entity to query its current total pain value.
/// </summary>
/// <param name="Pain">The accumulated pain value from all relevant sources. Initialized to zero and populated by providers.</param>
[ByRefEvent]
public record struct GetPainEvent(FixedPoint2 Pain) : IWoundRelayEvent
{
    public ProtoId<DamageTypePrototype>? Type { get; } = null;
}

/// <summary>
/// Event raised on an entity after its total pain value has been changed.
/// </summary>
/// <param name="Painful">This is the entity whose pain was changed.</param>
/// <param name="Pain">The amount by which the pain has changed.</param>
public record struct PainChangedEvent(Entity<PainfulComponent> Painful, FixedPoint2 Pain) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised on an entity after its pain level has been changed.
/// </summary>
/// <param name="Level">The new pain level.</param>
/// <param name="Location">The specific body location when pain level changed.</param>
public record struct PainLevelChangedEvent(PainLevel Level, BodyProviderType Location = BodyProviderType.All);
