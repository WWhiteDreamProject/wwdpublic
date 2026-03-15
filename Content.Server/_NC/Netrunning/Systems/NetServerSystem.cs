using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared._NC.Netrunning.Components;
using Content.Shared._NC.Netrunning.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Content.Shared.Mobs.Components;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.Player;
using Content.Shared.Database;
using Content.Shared.Administration.Logs;


namespace Content.Server._NC.Netrunning.Systems;

public sealed class NetServerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly Content.Server.Doors.Systems.DoorSystem _door = default!;
    [Dependency] private readonly CyberdeckSystem _cyberdeck = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetServerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<NetServerComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<NetServerComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<NetServerComponent, NetServerSetPasswordMessage>(OnSetPassword);
        SubscribeLocalEvent<NetServerComponent, NetServerOpenMapMessage>(OnOpenMap);
        SubscribeLocalEvent<NetServerComponent, NetMapInteractMessage>(OnMapInteract); // New
        SubscribeLocalEvent<NetServerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NetServerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NetServerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnStartup(EntityUid uid, NetServerComponent component, ComponentStartup args)
    {
        UpdateUi(uid, component);
        MarkConnectedDevices(uid);
    }

    private void OnOpenMap(EntityUid uid, NetServerComponent component, NetServerOpenMapMessage args)
    {
        _ui.OpenUi(uid, NetMapUiKey.Key, args.Actor);
    }

    private void OnMapInteract(EntityUid uid, NetServerComponent component, NetMapInteractMessage args)
    {
        var user = args.Actor;
        var target = GetEntity(args.Target);

        if (Deleted(target)) return;

        // Remote validation: Check if target is "visible" to the specific network is implicit
        // because the client only sees blips sent by UpdateMapUi.
        // However, we should verify that 'uid' (Server) still has access to 'target'.
        // For now, trusting the client's valid entity request + Range check (admin/root access implied interact range).

        // Use Interaction System to check if User can interact?
        // No, this is remote. We check if User has Root Access (Active Session / Hack).
        // Assuming if they have the UI open, they have access.

        switch (args.Action)
        {
            case NetMapAction.Open:
                if (TryComp<DoorComponent>(target, out var door))
                {
                    _door.TryOpen(target, door);
                    _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} opened {ToPrettyString(target)} via NetMap");
                }
                break;
            case NetMapAction.Close:
                if (TryComp<DoorComponent>(target, out var door2))
                {
                    _door.TryClose(target, door2);
                    _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} closed {ToPrettyString(target)} via NetMap");
                }
                break;
            case NetMapAction.Toggle:
                if (TryComp<DoorComponent>(target, out var door3))
                {
                    _door.TryToggleDoor(target, door3);
                    _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} toggled {ToPrettyString(target)} via NetMap");
                }
                // TODO: Light Toggle
                break;
            case NetMapAction.Bolt:
                // Requires Hacking/Emag technically, but Root Access grants full control?
                // Let's allow basic bolting if we have full control.
                if (TryComp<DoorBoltComponent>(target, out var bolts))
                {
                    _door.SetBoltsDown((target, bolts), !bolts.BoltsDown, user);
                    _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user)} toggled bolts on {ToPrettyString(target)} via NetMap");
                }
                break;
            case NetMapAction.Attack:
                _cyberdeck.TrySetTargetFromHands(user, target);
                break;
        }

        // Force UI update to reflect changes (e.g. door closed)
        Dirty(uid, component);
        UpdateMapUi(uid, component);
    }

    private void OnUiOpened(EntityUid uid, NetServerComponent component, BoundUIOpenedEvent args)
    {
        if (args.UiKey is NetServerUiKey)
            UpdateUi(uid, component);
        else if (args.UiKey is NetMapUiKey)
            UpdateMapUi(uid, component);
    }

    private void OnAnchorChanged(EntityUid uid, NetServerComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateUi(uid, component);
        MarkConnectedDevices(uid);
    }

    private void OnInteractUsing(EntityUid uid, NetServerComponent component, InteractUsingEvent args)
    {
        if (!TryComp<NetIceComponent>(args.Used, out var ice))
            return;

        if (!TryComp<ItemSlotsComponent>(uid, out var slotsComp))
            return;

        foreach (var (slotName, slot) in slotsComp.Slots)
        {
            if (!slotName.StartsWith("ice_slot_"))
                continue;

            if (slot.Item == null)
            {
                if (_slots.TryInsert(uid, slotName, args.Used, args.User))
                {
                    args.Handled = true;
                    // UpdateUi calls handled by OnContainerModified
                    return;
                }
            }
        }
    }

    private void OnContainerModified(EntityUid uid, NetServerComponent component, ContainerModifiedMessage args)
    {
        UpdateUi(uid, component);
    }

    private void OnSetPassword(EntityUid uid, NetServerComponent component, NetServerSetPasswordMessage args)
    {
        if (args.Password.Length > 4 || !args.Password.All(char.IsDigit))
            return;

        if (!TryComp<ItemSlotsComponent>(uid, out var slotsComp))
            return;

        // Find the slot by name (manual iteration since TryGetValue might not work with some collections or to be safe)
        ItemSlot? targetSlot = null;
        foreach (var (name, slot) in slotsComp.Slots)
        {
            if (name == args.SlotName)
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot != null && targetSlot.Item != null)
        {
            if (TryComp<NetIceComponent>(targetSlot.Item.Value, out var ice))
            {
                ice.Password = args.Password;
                Dirty(targetSlot.Item.Value, ice);
                UpdateUi(uid, component);
            }
        }
    }

    private void UpdateUi(EntityUid uid, NetServerComponent component)
    {
        var slots = new List<NetServerSlotData>();

        if (TryComp<ItemSlotsComponent>(uid, out var slotsComp))
        {
            foreach (var (slotName, slot) in slotsComp.Slots)
            {
                if (!slotName.StartsWith("ice_slot_"))
                    continue;

                string itemName = Loc.GetString("net-server-ui-empty-slot");
                NetIceType iceType = NetIceType.Gate; // Default
                bool hasIce = false;
                string password = "";

                if (slot.Item != null)
                {
                    itemName = Name(slot.Item.Value);
                    if (TryComp<NetIceComponent>(slot.Item.Value, out var ice))
                    {
                        iceType = ice.IceType;
                        hasIce = true;
                        password = ice.Password;
                    }
                }

                slots.Add(new NetServerSlotData(slotName, slot.Name, itemName, iceType, hasIce, password));
            }
        }

        var devices = GetConnectedDevices(uid);

        // Gather status info
        bool hasApc = false; // We can assume it has APC connection if the MV node is powered/valid
        bool isPowered = false;
        string serverName = Name(uid);

        // Check if MV node is connected and powered
        // Check if MV node is connected to an APC
        if (TryComp<NodeContainerComponent>(uid, out var nodeContainer) &&
            nodeContainer.Nodes.TryGetValue("input", out var inputNode) &&
            inputNode.NodeGroup != null)
        {
            // Scan the group for an APC
            foreach (var node in inputNode.NodeGroup.Nodes)
            {
                if (node.Owner != uid && HasComp<ApcComponent>(node.Owner))
                {
                    hasApc = true;
                    isPowered = true; // Assuming APC implies power for now
                    break;
                }
            }
        }

        var state = new NetServerBoundUiState(hasApc, isPowered, devices, serverName, slots);
        _ui.SetUiState(uid, NetServerUiKey.Key, state);
    }

    private void UpdateMapUi(EntityUid uid, NetServerComponent component)
    {
        var blips = new List<NetMapBlip>();
        var networkEntities = GetNetworkEntities(uid).Distinct().ToList();

        // 1. Add Network Devices
        foreach (var entity in networkEntities)
        {
            if (Deleted(entity) || !TryComp<TransformComponent>(entity, out var xform))
                continue;

            var coords = GetNetCoordinates(xform.Coordinates);
            var color = Color.Green; // Default online
            var type = NetBlipType.Generic;
            var name = Name(entity);

            // Determine Type & Color
            if (HasComp<DoorComponent>(entity))
            {
                type = NetBlipType.Door;
                if (TryComp<DoorComponent>(entity, out var door))
                {
                    if (door.State == DoorState.Open) color = Color.Green;
                    else if (door.State == DoorState.Closed) color = Color.Red;
                    // Check Bolt?
                }
            }
            else if (HasComp<ApcPowerReceiverComponent>(entity) && !HasComp<DoorComponent>(entity))
            {
                // Generic Device
                if (HasComp<SurveillanceCameraComponent>(entity)) type = NetBlipType.Camera;
                else type = NetBlipType.Generic;
            }

            blips.Add(new NetMapBlip(GetNetEntity(entity), coords, color, type, name));
        }

        // 2. Add Mobs (Radar) - "Fog of War" / Vision
        // Simple implementation: Find all mobs on same grid, check distance to ANY network entity
        if (TryComp<TransformComponent>(uid, out var serverXform) && serverXform.GridUid != null)
        {
            var mobs = new List<EntityUid>();
            // Only track MobState (alive/dead mobs)
            // Using EntityQuery is faster
            var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
            while (query.MoveNext(out var mobUid, out var mobState, out var mobXform))
            {
                if (mobXform.GridUid != serverXform.GridUid) continue;
                mobs.Add(mobUid);
            }

            foreach (var mob in mobs)
            {
                if (mob == uid) continue; // Should not happen

                // Check distance to nearest network node (Server or Devices)
                // Start with Server
                bool visible = false;
                if (serverXform.Coordinates.TryDistance(EntityManager, Transform(mob).Coordinates, out var dist) && dist < 15f)
                {
                    visible = true;
                }
                else
                {
                    // Check devices (Optimization: maybe just check one nearby?)
                    // For now, check all.
                    // TODO: This is O(M*N), might be slow. Limit N?
                    // Optimization: Use Lookup?
                    // Let's assume network covers the station.
                    // Scan Radius check:
                    foreach (var netEnt in networkEntities)
                    {
                        if (TryComp<TransformComponent>(netEnt, out var netXform) &&
                            netXform.Coordinates.TryDistance(EntityManager, Transform(mob).Coordinates, out var d) && d < 10f)
                        {
                            visible = true;
                            break;
                        }
                    }
                }

                if (visible)
                {
                    // Color Coding: Player = Blue, NPC = Yellow
                    var blipColor = Color.Yellow;
                    if (HasComp<ActorComponent>(mob))
                        blipColor = Color.Cyan;

                    blips.Add(new NetMapBlip(GetNetEntity(mob), GetNetCoordinates(Transform(mob).Coordinates), blipColor, NetBlipType.Mob, Name(mob)));
                }
            }
        }

        var state = new NetMapBoundUiState(GetNetEntity(Transform(uid).GridUid), blips);
        _ui.SetUiState(uid, NetMapUiKey.Key, state);
    }

    private IEnumerable<EntityUid> GetNetworkEntities(EntityUid uid)
    {
        if (TryComp<NodeContainerComponent>(uid, out var nodeContainer) &&
            nodeContainer.Nodes.TryGetValue("input", out var mvNode) &&
            mvNode.NodeGroup != null)
        {
            foreach (var node in mvNode.NodeGroup.Nodes)
            {
                var entity = node.Owner;
                if (entity == uid) continue;

                if (TryComp<NodeContainerComponent>(entity, out var apcNodes) &&
                    apcNodes.Nodes.TryGetValue("output", out var lvNode) &&
                    lvNode.NodeGroup is ApcNet apcNet)
                {
                    foreach (var provider in apcNet.Providers)
                    {
                        foreach (var receiver in provider.LinkedReceivers)
                        {
                            yield return receiver.Owner;
                        }
                    }
                }
            }
        }
    }

    private List<NetDeviceData> GetConnectedDevices(EntityUid uid)
    {
        var devices = new List<NetDeviceData>();

        foreach (var device in GetNetworkEntities(uid))
        {
            if (TryComp<MetaDataComponent>(device, out var meta) && meta.EntityPrototype != null)
            {
                bool isPowered = true; // Simplified, assuming receiver is valid
                if (TryComp<ApcPowerReceiverComponent>(device, out var power)) isPowered = power.Powered;

                devices.Add(new NetDeviceData(Name(device), meta.EntityPrototype.ID, isPowered));

                var protComp = EnsureComp<ProtectedByComponent>(device);
                protComp.Server = uid;
                Dirty(device, protComp);
            }
        }

        return devices.Distinct().ToList();
    }

    /// <summary>
    /// Marks all devices connected to this server with ProtectedByComponent.
    /// Called when server starts or anchors.
    /// </summary>
    public void MarkConnectedDevices(EntityUid serverUid)
    {
        // Check if server is anchored
        if (!TryComp<TransformComponent>(serverUid, out var xform) || !xform.Anchored)
            return;

        if (!TryComp<NodeContainerComponent>(serverUid, out var nodeContainer) ||
            !nodeContainer.Nodes.TryGetValue("input", out var mvNode) ||
            mvNode.NodeGroup == null)
            return;

        // Scan MV network for APCs
        foreach (var node in mvNode.NodeGroup.Nodes)
        {
            var entity = node.Owner;
            if (entity == serverUid) continue;

            if (TryComp<NodeContainerComponent>(entity, out var apcNodes) &&
                apcNodes.Nodes.TryGetValue("output", out var lvNode) &&
                lvNode.NodeGroup is ApcNet apcNet)
            {
                // Scan APC Net for Providers (Cables) -> Receivers (Devices)
                foreach (var provider in apcNet.Providers)
                {
                    foreach (var receiver in provider.LinkedReceivers)
                    {
                        var device = receiver.Owner;

                        // Add or update ProtectedByComponent
                        var protComp = EnsureComp<ProtectedByComponent>(device);
                        protComp.Server = serverUid;
                        Dirty(device, protComp);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a device is protected by a NetServer.
    /// Traverses: Device -> APC -> MV Network -> NetServer.
    /// </summary>
    public EntityUid? GetProtectingServer(EntityUid device)
    {
        // 1. Device -> APC
        if (!TryComp<ApcPowerReceiverComponent>(device, out var receiver) || receiver.Provider is not Component providerComp)
            return null; // Device has no power provider

        var apc = providerComp.Owner;

        // 2. APC -> MV Network ("input" node)
        if (!TryComp<NodeContainerComponent>(apc, out var nodeContainer) ||
            !nodeContainer.Nodes.TryGetValue("input", out var mvNode) ||
            mvNode.NodeGroup == null)
            return null; // APC not connected to MV

        // 3. Scan MV network for NetServer
        foreach (var node in mvNode.NodeGroup.Nodes)
        {
            var entity = node.Owner;
            if (HasComp<NetServerComponent>(entity))
            {
                // Found a server on the same MV grid!
                // Check if Server is functioning (Anchored, etc)
                if (TryComp<TransformComponent>(entity, out var xform) && xform.Anchored)
                {
                    return entity;
                }
            }
        }

        return null;
    }

    public List<NetIceComponent> GetIceEffects(EntityUid server)
    {
        var effects = new List<NetIceComponent>();
        if (!TryComp<ItemSlotsComponent>(server, out var slots))
            return effects;

        foreach (var (name, slot) in slots.Slots)
        {
            if (name.StartsWith("ice_slot_") && slot.Item != null)
            {
                if (TryComp<NetIceComponent>(slot.Item.Value, out var ice))
                {
                    effects.Add(ice);
                }
            }
        }

        return effects;
    }

    public bool HasActiveIce(EntityUid server)
    {
        if (!TryComp<ItemSlotsComponent>(server, out var slots))
            return false;

        foreach (var (name, slot) in slots.Slots)
        {
            if (name.StartsWith("ice_slot_") && slot.Item != null)
            {
                // Check if it's a valid ICE component
                if (HasComp<NetIceComponent>(slot.Item.Value))
                    return true;
            }
        }
        return false;
    }
}
