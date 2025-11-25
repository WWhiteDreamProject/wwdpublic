using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Pain.Systems;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, GetPainEvent>(RelayEventToBodyPart);
    }

    protected void RelayEventToBodyPart<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        foreach (var organ in GetBodyParts(body.AsNullable()))
            RaiseLocalEvent(organ, ref args);
    }

    protected void RelayEventToOrgans<T>(Entity<BodyComponent> body, ref T args) where T : struct
    {
        foreach (var organ in GetOrgans(body.AsNullable()))
            RaiseLocalEvent(organ, ref args);
    }
}
