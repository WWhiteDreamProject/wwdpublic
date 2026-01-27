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
using System.Linq;

namespace Content.Server._NC.Netrunning.Systems;

public sealed class NetServerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetServerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<NetServerComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<NetServerComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<NetServerComponent, NetServerSetPasswordMessage>(OnSetPassword);
        SubscribeLocalEvent<NetServerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NetServerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NetServerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnStartup(EntityUid uid, NetServerComponent component, ComponentStartup args)
    {
        UpdateUi(uid, component);
        MarkConnectedDevices(uid);
    }

    private void OnUiOpened(EntityUid uid, NetServerComponent component, BoundUIOpenedEvent args)
    {
        UpdateUi(uid, component);
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

                string itemName = "Empty";
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

    private List<NetDeviceData> GetConnectedDevices(EntityUid uid)
    {
        var devices = new List<NetDeviceData>();

        if (TryComp<NodeContainerComponent>(uid, out var nodeContainer) &&
            nodeContainer.Nodes.TryGetValue("input", out var mvNode) &&
            mvNode.NodeGroup != null)
        {
            // 1. Scan MV network for APCs
            foreach (var node in mvNode.NodeGroup.Nodes)
            {
                var entity = node.Owner;
                if (entity == uid) continue;

                if (TryComp<NodeContainerComponent>(entity, out var apcNodes) &&
                    apcNodes.Nodes.TryGetValue("output", out var lvNode) &&
                    lvNode.NodeGroup is ApcNet apcNet)
                {
                    // 2. Scan APC Net for Providers (Cables) -> Receivers (Devices)
                    foreach (var provider in apcNet.Providers)
                    {
                        foreach (var receiver in provider.LinkedReceivers)
                        {
                            var device = receiver.Owner;
                            if (TryComp<MetaDataComponent>(device, out var meta) && meta.EntityPrototype != null)
                            {
                                devices.Add(new NetDeviceData(Name(device), meta.EntityPrototype.ID, receiver.Powered));

                                // Mark device as protected by this server
                                var protComp = EnsureComp<ProtectedByComponent>(device);
                                protComp.Server = uid;
                                Dirty(device, protComp);
                            }
                        }
                    }
                }
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
