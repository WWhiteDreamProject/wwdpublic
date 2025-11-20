using Content.Shared._White.Body.Components;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    protected void RelayEventToOrgans<T>(Entity<BodyComponent> body, ref T args) where T : EntityEventArgs
    {
        foreach (var organ in GetOrgans(body.AsNullable()))
            RaiseLocalEvent(organ, ref args);
    }
}
