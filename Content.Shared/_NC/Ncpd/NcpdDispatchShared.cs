using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Content.Shared._NC.CitiNet;

namespace Content.Shared._NC.Ncpd
{
    [Serializable, NetSerializable]
    public enum NcpdTabletUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public struct NcpdCallData
    {
        public int Id;
        public string Title;
        public string Sector;
        public string Description;
        public NetCoordinates Coordinates;
        public TimeSpan CreatedTime;

        public NcpdCallData(int id, string title, string sector, string description, NetCoordinates coordinates, TimeSpan createdTime)
        {
            Id = id;
            Title = title;
            Sector = sector;
            Description = description;
            Coordinates = coordinates;
            CreatedTime = createdTime;
        }
    }

    [Serializable, NetSerializable]
    public sealed class NcpdTabletState : BoundUserInterfaceState
    {
        public List<NcpdCallData> ActiveCalls;
        public int? SelectedCallId;
        public NetEntity? GridUid;

        // CitiNet Map data
        public List<CitiNetMapSectorData> Sectors;
        public List<CitiNetMapBeaconData> Beacons;
        public List<CitiNetMapPingData> Pings;

        public NcpdTabletState(
            List<NcpdCallData> activeCalls, 
            int? selectedCallId = null,
            NetEntity? gridUid = null,
            List<CitiNetMapSectorData>? sectors = null,
            List<CitiNetMapBeaconData>? beacons = null,
            List<CitiNetMapPingData>? pings = null)
        {
            ActiveCalls = activeCalls;
            SelectedCallId = selectedCallId;
            GridUid = gridUid;
            Sectors = sectors ?? new();
            Beacons = beacons ?? new();
            Pings = pings ?? new();
        }
    }

    [Serializable, NetSerializable]
    public sealed class NcpdTabletSelectCallMsg : BoundUserInterfaceMessage
    {
        public int CallId;

        public NcpdTabletSelectCallMsg(int callId)
        {
            CallId = callId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class NcpdTabletClearCallMsg : BoundUserInterfaceMessage
    {
        public int CallId;

        public NcpdTabletClearCallMsg(int callId)
        {
            CallId = callId;
        }
    }

    // Сообщение от консолей диспетчера
    [Serializable, NetSerializable]
    public sealed class NcpdDispatchCallMsg : BoundUserInterfaceMessage
    {
        public string Title;
        public string Sector;
        public string Description;
        public NetCoordinates Coordinates;

        public NcpdDispatchCallMsg(string title, string sector, string description, NetCoordinates coordinates)
        {
            Title = title;
            Sector = sector;
            Description = description;
            Coordinates = coordinates;
        }
    }
}
