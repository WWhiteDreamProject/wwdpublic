using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;


namespace Content.Shared._White.Bloodstream.Systems;

public abstract class SharedMetabolizerSystem : EntitySystem
{
    protected EntityQuery<BloodstreamComponent> BloodstreamQuery;

    public override void Initialize()
    {
        base.Initialize();

        BloodstreamQuery = GetEntityQuery<BloodstreamComponent>();
    }

    #region Public API

    /// <summary>
    /// Updates the metabolic rate for a given entity,
    /// Raises to retrieve the base rate, to find the active multiplier, and then to update relevant components based on these results.
    /// </summary>
    public void UpdateMetabolicRate(Entity<BloodstreamComponent?> ent)
    {
        if (!BloodstreamQuery.Resolve(ent, ref ent.Comp))
            return;

        var getRateEv = new GetMetabolicRateEvent();
        RaiseLocalEvent(ent, ref getRateEv);

        var getMultiplierEv = new GetMetabolicMultiplierEvent();
        RaiseLocalEvent(ent, ref getMultiplierEv);

        var rate = getRateEv.Rate * getMultiplierEv.Multiplier;
        if (ent.Comp.MetabolicRate == rate)
            return;

        ent.Comp.MetabolicRate = rate;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.MetabolicRate));

        ent.Comp.UpdateIntervalMultiplier = rate == 0 ? 0 : 1 / rate;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.UpdateIntervalMultiplier));

        var changedEv = new MetabolicRateChangedEvent(rate);
        RaiseLocalEvent(ent, ref changedEv);
    }

    #endregion
}

/// <summary>
/// Event raised on an entity to determine their metabolic multiplier.
/// </summary>
/// <param name="Multiplier">What the metabolism's rate will be multiplied by.</param>
[ByRefEvent]
public record struct GetMetabolicMultiplierEvent(float Multiplier = 1f);

/// <summary>
/// Event raised on an entity to determine their metabolic rate.
/// </summary>
/// <param name="Rate">What the metabolism's rate.</param>
[ByRefEvent]
public record struct GetMetabolicRateEvent(float Rate = 0f) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised on an entity to apply their metabolic multiplier to relevant systems.
/// Note that you should be storing this value as to not accrue precision errors when it's modified.
/// </summary>
/// <param name="Rate">New metabolism's rate.</param>
[ByRefEvent]
public readonly record struct MetabolicRateChangedEvent(float Rate) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}
