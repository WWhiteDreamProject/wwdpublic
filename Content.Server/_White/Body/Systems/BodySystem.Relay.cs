using Content.Server._White.Respirator.Systems;
using Content.Shared._White.Body.Components;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<BodyComponent, CanMetabolizeGasEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, ExhaleEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, GetBreathVolumeEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, GetSaturationConsumption>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, InhaleEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, SaturationLevelChangedEvent>(RelayEvent);
        SubscribeLocalEvent<BodyComponent, SuffocationChangedEvent>(RelayEvent);
    }
}
