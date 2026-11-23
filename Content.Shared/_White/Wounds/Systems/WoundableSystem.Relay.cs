using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Pain.Systems;
using Content.Shared._White.Wounds.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<WoundableProviderComponent, GetBleedingEvent>(RelayEvent);
        SubscribeLocalEvent<WoundableProviderComponent, GetPainEvent>(RelayEvent);

        SubscribeLocalEvent<WoundableComponent, ModifyBleedingEvent>(RelayEvent);
    }

    private void RelayEvent<T>(Entity<WoundableComponent> ent, ref T args) where T : IWoundRelayEvent
    {
        var wounds = GetWounds(ent.AsNullable(), args.Type);
        var ev = new WoundRelayedEvent<T>(args)
        {
            Count = wounds.Count
        };

        foreach (var wound in wounds)
        {
            ev.Wound = wound.Comp;
            RaiseLocalEvent(wound, ref ev);
        }

        args = ev.Args;
    }

    private void RelayEvent<T>(Entity<WoundableProviderComponent> ent, ref T args) where T : IWoundRelayEvent
    {
        var wounds = GetWounds(ent.AsNullable(), args.Type);
        var ev = new WoundRelayedEvent<T>(args)
        {
            Count = wounds.Count
        };

        foreach (var wound in wounds)
        {
            ev.Wound = wound.Comp;
            RaiseLocalEvent(wound, ref ev);
        }

        args = ev.Args;
    }
}

/// <summary>
/// An event wrapper for passing events related to wound.
/// </summary>
[ByRefEvent]
public record struct WoundRelayedEvent<TEvent>(TEvent Args)
{
    /// <summary>
    /// The number of wounds to which the event was relayed
    /// </summary>
    public int Count = 0;

    /// <summary>
    /// The relayed event.
    /// </summary>
    public TEvent Args = Args;

    /// <summary>
    /// This is the component to which the event was relayed.
    /// </summary>
    public WoundComponent Wound = default!;
}

/// <summary>
/// Events that should be relayed to wounds should implement this interface.
/// </summary>
public interface IWoundRelayEvent
{
    /// <summary>
    /// What wound should this event be relayed to?
    /// </summary>
    ProtoId<DamageTypePrototype>? Type { get; }
}

