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

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CitiNetMapComponent : Component 
{
    [DataField("visibleGroups"), AutoNetworkedField]
    public List<string> VisibleGroups = new() { "Public" };
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MapSectorComponent : Component
{
    [DataField("name"), AutoNetworkedField] public string SectorName = "Unknown Sector";
    [DataField("color"), AutoNetworkedField] public Color Color = Color.DimGray;
    [DataField("bounds"), AutoNetworkedField] public Box2 Bounds = new(-10, -10, 10, 10);
    [DataField("visibleInWorld"), AutoNetworkedField] public bool VisibleInWorld = false;
    [DataField("fontSize"), AutoNetworkedField] public int FontSize = 12;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MapBeaconComponent : Component
{
    [DataField("label"), AutoNetworkedField] public string Label = string.Empty;
    [DataField("icon"), AutoNetworkedField] public SpriteSpecifier? Icon;
    [DataField("color"), AutoNetworkedField] public Color Color = Color.White;
    [DataField("visible"), AutoNetworkedField] public bool IsVisible = true;
    [DataField("visibleInWorld"), AutoNetworkedField] public bool VisibleInWorld = false;
    [DataField("fontSize"), AutoNetworkedField] public int FontSize = 10;
    [DataField("group")] public string Group = "Public"; 
    [DataField("requiredRole")] public string RequiredRole = string.Empty; 
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
    public int FontSize;

    public CitiNetMapSectorData(string name, Color color, Box2 bounds, int fontSize)
    {
        Name = name;
        Color = color;
        Bounds = bounds;
        FontSize = fontSize;
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
    public int FontSize;
    public bool IsDead;
    public bool IsSelf;

    public CitiNetMapBeaconData(NetEntity netEnt, string label, SpriteSpecifier? icon, Color color, Vector2 localPosition, int fontSize, bool isDead = false, bool isSelf = false)
    {
        NetEnt = netEnt;
        Label = label;
        Icon = icon;
        Color = color;
        LocalPosition = localPosition;
        FontSize = fontSize;
        IsDead = isDead;
        IsSelf = isSelf;
    }
}

[Serializable, NetSerializable]
public enum CitiNetPingType : byte
{
    Generic,
    Gunshot,
    SOS,
    Tracker
}

[Serializable, NetSerializable, DataRecord]
public struct CitiNetMapPingData
{
    public Vector2 LocalPosition;
    public Color Color;
    public float Radius;
    public CitiNetPingType Type;

    public CitiNetMapPingData(Vector2 localPosition, Color color, float radius, CitiNetPingType type = CitiNetPingType.Generic)
    {
        LocalPosition = localPosition;
        Color = color;
        Radius = radius;
        Type = type;
    }
}
