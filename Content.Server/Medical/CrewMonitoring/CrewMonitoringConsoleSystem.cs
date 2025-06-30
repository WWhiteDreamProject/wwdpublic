using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.PowerCell;
using Content.Shared.Item;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!; // WWDP EDIT
    [Dependency] private readonly SharedPopupSystem _popup = default!; // WWDP EDIT

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs); // WWDP EDIT
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        component.ConnectedSensors = sensorStatus;
        UpdateUserInterface(uid, component);
        SendNotification(uid, component); // WWDP EDIT
    }

    // WWDP EDIT START
    private void SendNotification(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        foreach (var sensor in component.ConnectedSensors.Values)
        {
            if (!_cell.TryGetBatteryFromSlot(uid, out var battery))
                return;

            if (battery.CurrentCharge < 20.0f)
                return;

            var index = sensor.DamagePercentage;

            if (index >= 0.8f && component.SoundNotification && sensor.IsAlive)
            {
                _audio.PlayEntity(
                    "/Audio/Items/beep.ogg",
                    Filter.Pvs(uid),
                    uid,
                    false,
                    AudioParams.Default.WithMaxDistance(1));
                break;
            }
        }

    }

    private void OnGetVerbs(EntityUid uid, CrewMonitoringConsoleComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<ItemComponent>(uid, out var itemComponent))
            return;

        args.Verbs.Add(new ()
        {
            Text = Loc.GetString("crew-portable-monitoring-notification"),
            Priority = -3,
            Act = () =>
            {
                comp.SoundNotification = !comp.SoundNotification;

                _popup.PopupEntity(
                    comp.SoundNotification
                        ? Loc.GetString("crew-portable-monitoring-notification-on")
                        : Loc.GetString("crew-portable-monitoring-notification-off"),
                    uid,
                    args.User);
            }
        });
    }
    // WWDP EDIT END

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        var allSensors = component.ConnectedSensors.Values.ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }
}
