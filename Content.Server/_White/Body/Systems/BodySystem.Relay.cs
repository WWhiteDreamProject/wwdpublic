using Content.Server._White.Body.Respirator.Systems;
using Content.Shared._White.Body.Components;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, AfterSaturationLevelChangedEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, BeforeBreathEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, BeforeUpdateRespiratorEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, CanMetabolizeGasEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, ExhaledGasEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, InhaledGasEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, StopSuffocatingEvent>(RelayEventToOrgans);
        SubscribeLocalEvent<BodyComponent, SuffocationEvent>(RelayEventToOrgans);
    }
}
