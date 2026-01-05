using Content.Shared._White.Body.Bloodstream.Systems;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Pain.Systems;
using Content.Shared.Rejuvenate;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, GetBleedEvent>(RelayEventToBodyParts);
        SubscribeLocalEvent<BodyComponent, GetPainEvent>(RelayEventToBodyParts);

        SubscribeLocalEvent<BodyComponent, AfterBloodAmountChangedEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicRateEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, GetMetabolicMultiplierEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, GetMetabolicRateEvent>(RelayEventToOrgans);

        SubscribeLocalEvent<BodyComponent, GetBloodReductionEvent>(RelayEventToAll);
        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(RelayEventToAll);
    }

    protected void RelayEventToBones<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        var ev = new BoneRelayedEvent<T>(body, args);
        foreach (var organ in GetBones(body.AsNullable()))
            RaiseLocalEvent(organ, ref ev);
        args = ev.Args;
    }

    protected void RelayEventToBones<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        var ev = new BoneRelayedEvent<T>((uid, component), args);
        foreach (var organ in GetBones((uid, component)))
            RaiseLocalEvent(organ, ref ev);
    }

    protected void RelayEventToBodyParts<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        var ev = new BodyPartRelayedEvent<T>(body, args);
        foreach (var organ in GetBodyParts(body.AsNullable()))
            RaiseLocalEvent(organ, ref ev);
        args = ev.Args;
    }

    protected void RelayEventToBodyParts<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        var ev = new BodyPartRelayedEvent<T>((uid, component), args);
        foreach (var organ in GetBodyParts((uid, component)))
            RaiseLocalEvent(organ, ref ev);
    }

    protected void RelayEventToOrgans<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        var ev = new OrganRelayedEvent<T>(body, args);
        foreach (var organ in GetOrgans(body.AsNullable()))
            RaiseLocalEvent(organ, ref ev);
        args = ev.Args;
    }

    protected void RelayEventToOrgans<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        var ev = new OrganRelayedEvent<T>((uid, component), args);
        foreach (var organ in GetOrgans((uid, component)))
            RaiseLocalEvent(organ, ref ev);
    }

    protected void RelayEventToAll<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        RelayEventToBodyParts(body, ref args);
        RelayEventToBones(body, ref args);
        RelayEventToOrgans(body, ref args);
    }

    protected void RelayEventToAll<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        RelayEventToBodyParts(uid, component, args);
        RelayEventToBones(uid, component, args);
        RelayEventToOrgans(uid, component, args);
    }
}
