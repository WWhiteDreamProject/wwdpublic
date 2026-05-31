using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Nutrition.Systems;
using Content.Shared._White.Pain.Systems;
using Content.Shared._White.Wounds.Systems;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, BloodAmountChangedEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, GetMetabolicRateEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, GetWoundableDamageEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, MetabolicRateChangedEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, PainChangedEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, TryIngestEvent>(RelayEvent);

        SubscribeLocalEvent<BodyProviderComponent, GetWoundableDamageEvent>(RelayEvent);
        SubscribeLocalEvent<BodyProviderComponent, WoundableDamageChangedEvent>(RelayEvent);
    }

    protected void RelayEvent<T>(Entity<BodyComponent> ent, ref T args) where T : IBodyRelayEvent
    {
        var ev = new BodyRelayedEvent<T>(args);

        foreach (var provider in GetProviders(ent.AsNullable(), args.Type))
        {
            ev.Provider = provider.Comp;
            RaiseLocalEvent(provider, ref ev);
        }

        args = ev.Args;
    }

    protected void RelayEvent<T>(Entity<BodyProviderComponent> ent, ref T args) where T : IBodyRelayEvent
    {
        var ev = new BodyRelayedEvent<T>(args);

        foreach (var provider in GetProviders(ent.AsNullable(), args.Type))
        {
            ev.Provider = provider.Comp;
            RaiseLocalEvent(provider, ref ev);
        }

        args = ev.Args;
    }
}

/// <summary>
/// An event wrapper for passing events related to body provider.
/// </summary>
[ByRefEvent]
public record struct BodyRelayedEvent<TEvent>(TEvent Args)
{
    /// <summary>
    /// This is the component to which the event was relayed.
    /// </summary>
    public BodyProviderComponent Provider = default!;
}

/// <summary>
/// Events that should be relayed to body providers should implement this interface.
/// </summary>
public interface IBodyRelayEvent
{
    /// <summary>
    /// What body providers should this event be relayed to.
    /// </summary>
    public BodyProviderType Type { get; }
}
