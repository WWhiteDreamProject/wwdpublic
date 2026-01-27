using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System;

namespace Content.Shared._NC.Netrunning.UI;

[Serializable, NetSerializable]
public enum NetMapUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum NetBlipType : byte
{
    Generic,
    Door,
    Camera,
    Apc,
    Mob
}

[Serializable, NetSerializable]
public struct NetMapBlip
{
    public NetCoordinates Coordinates;
    public Color Color;
    public NetBlipType BlipType;
    public string Name;
    public NetEntity Entity;
    public bool IsBlinking;

    public NetMapBlip(NetEntity entity, NetCoordinates coords, Color color, NetBlipType type, string name, bool blink = false)
    {
        Entity = entity;
        Coordinates = coords;
        Color = color;
        BlipType = type;
        Name = name;
        IsBlinking = blink;
    }
}

[Serializable, NetSerializable]
public sealed class NetMapBoundUiState : BoundUserInterfaceState
{
    public NetEntity? TargetGrid;
    public List<NetMapBlip> Blips;

    public NetMapBoundUiState(NetEntity? targetGrid, List<NetMapBlip> blips)
    {
        TargetGrid = targetGrid;
        Blips = blips;
    }
}

[Serializable, NetSerializable]
public enum NetMapAction : byte
{
    Open,
    Close,
    Toggle,
    Bolt,
    Shock,
    Tag,
    Attack,
    ViewFeed
}

[Serializable, NetSerializable]
public sealed class NetMapInteractMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;
    public NetMapAction Action;

    public NetMapInteractMessage(NetEntity target, NetMapAction action)
    {
        Target = target;
        Action = action;
    }
}
