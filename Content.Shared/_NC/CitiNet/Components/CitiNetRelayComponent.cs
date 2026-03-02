using Robust.Shared.GameStates;

namespace Content.Shared._NC.CitiNet.Components;

/// <summary>
/// Маркер CitiNet Relay — локальный городской узел связи.
/// Маршрутизирует гражданские коммуникации (звонки, BBS-каналы).
/// Требует питания через ApcPowerReceiverComponent.
/// При обесточивании все CitiNet-функции в зоне падают.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CitiNetRelayComponent : Component
{
}
