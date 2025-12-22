using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Pain.Systems;
using Content.Shared.Rejuvenate;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, GetPainEvent>(RelayEventToBodyParts);

        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(RelayEventToAll);
    }

    protected void RelayEventToBodyParts<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        foreach (var organ in GetBodyParts(body.AsNullable()))
            RaiseLocalEvent(organ, ref args);
    }

    protected void RelayEventToBodyParts<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        foreach (var organ in GetBodyParts((uid, component)))
            RaiseLocalEvent(organ, args);
    }

    protected void RelayEventToBones<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        foreach (var organ in GetBones(body.AsNullable()))
            RaiseLocalEvent(organ, ref args);
    }

    protected void RelayEventToBones<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        foreach (var organ in GetBones((uid, component)))
            RaiseLocalEvent(organ, args);
    }

    protected void RelayEventToOrgans<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        foreach (var organ in GetOrgans(body.AsNullable()))
            RaiseLocalEvent(organ, ref args);
    }

    protected void RelayEventToOrgans<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        foreach (var organ in GetOrgans((uid, component)))
            RaiseLocalEvent(organ, args);
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
