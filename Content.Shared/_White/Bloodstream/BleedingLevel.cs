using Robust.Shared.Serialization;

namespace Content.Shared._White.Bloodstream;

[Serializable, NetSerializable]
public enum BleedingLevel : byte
{
    None,
    Zero,
    Mild,
    Moderate,
    Severe,
    Mortal,
}
