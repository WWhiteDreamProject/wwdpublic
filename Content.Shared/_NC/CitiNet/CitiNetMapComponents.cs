using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._NC.CitiNet;

[RegisterComponent, NetworkedComponent]
public sealed partial class CitiNetMapCartridgeComponent : Component { }

[RegisterComponent, NetworkedComponent]
public sealed partial class CitiNetMapComponent : Component { }

// This component is for sectors/zones on the map (like "Corporate District")
[RegisterComponent, NetworkedComponent]
public sealed partial class MapSectorComponent : Component
{
    [DataField("name")] public string SectorName = "Unknown Sector";
    [DataField("color")] public Color Color = Color.DimGray;
    [DataField("bounds")] public Box2 Bounds = new(-10, -10, 10, 10);
}

// This component is for points of interest (POI)
[RegisterComponent, NetworkedComponent]
public sealed partial class MapBeaconComponent : Component
{
    [DataField("label")] public string Label = string.Empty;
    [DataField("icon")] public SpriteSpecifier? Icon;
    [DataField("color")] public Color Color = Color.White;
    [DataField("visible")] public bool IsVisible = true;
}

[Serializable, NetSerializable]
public enum CitiNetMapUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class CitiNetMapBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly NetEntity? MapUid;
    public readonly List<CitiNetMapSectorData> Sectors;
    public readonly List<CitiNetMapBeaconData> Beacons;
    public readonly List<CitiNetMapPingData> Pings;

    public CitiNetMapBoundUserInterfaceState(NetEntity? mapUid, List<CitiNetMapSectorData> sectors, List<CitiNetMapBeaconData> beacons, List<CitiNetMapPingData> pings)
    {
        MapUid = mapUid;
        Sectors = sectors;
        Beacons = beacons;
        Pings = pings;
    }
}

[Serializable, NetSerializable, DataRecord]
public struct CitiNetMapSectorData
{
    public string Name;
    public Color Color;
    public Box2 Bounds;

    public CitiNetMapSectorData(string name, Color color, Box2 bounds)
    {
        Name = name;
        Color = color;
        Bounds = bounds;
    }
}

[Serializable, NetSerializable, DataRecord]
public struct CitiNetMapBeaconData
{
    public NetEntity NetEnt;
    public string Label;
    public SpriteSpecifier? Icon;
    public Color Color;
    public Vector2 LocalPosition;

    public CitiNetMapBeaconData(NetEntity netEnt, string label, SpriteSpecifier? icon, Color color, Vector2 localPosition)
    {
        NetEnt = netEnt;
        Label = label;
        Icon = icon;
        Color = color;
        LocalPosition = localPosition;
    }
}

// Layer 4: Dynamic Pings (Radars, Trackers, Gunshots)
[RegisterComponent, NetworkedComponent]
public sealed partial class CitiNetPingComponent : Component
{
    [DataField("color")] public Color Color = Color.Red;
    [DataField("radius")] public float Radius = 2f;
}

[Serializable, NetSerializable, DataRecord]
public struct CitiNetMapPingData
{
    public Vector2 LocalPosition;
    public Color Color;
    public float Radius;

    public CitiNetMapPingData(Vector2 localPosition, Color color, float radius)
    {
        LocalPosition = localPosition;
        Color = color;
        Radius = radius;
    }
}
