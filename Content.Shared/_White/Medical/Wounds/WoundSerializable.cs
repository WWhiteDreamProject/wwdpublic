using Robust.Shared.Serialization;

namespace Content.Shared._White.Medical.Wounds;

[Serializable, NetSerializable]
public enum WoundSeverity : byte
{
    Healthy,
    Minor,
    Moderate,
    Severe,
    Critical,
}
