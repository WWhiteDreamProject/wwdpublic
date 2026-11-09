using Robust.Shared.Serialization;

namespace Content.Shared._White.Pain;

[Serializable, NetSerializable]
public enum PainLevel : byte
{
    None,
    Zero,
    Mild,
    Moderate,
    Severe,
    Excruciating,
    Mortal,
}
