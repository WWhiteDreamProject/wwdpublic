using Content.Server.Body.Systems;
using Content.Shared._White.Body.Components;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(RelayEventToOrgans);
    }
}
