using Robust.Shared.Serialization;

namespace Content.Shared._White.BloodCult;

[Serializable, NetSerializable]
public enum SoulShardVisualState : byte
{
    HasMind,
    Blessed,
    Sprite,
    Glow
}

[Serializable, NetSerializable]
public enum GenericCultVisuals : byte
{
    State, // True or False
    Layer
}

[Serializable, NetSerializable]
public enum PylonVisuals : byte
{
    Activated,
    Layer
}

[Serializable, NetSerializable]
public enum PentagramKey
{
    Key
}
