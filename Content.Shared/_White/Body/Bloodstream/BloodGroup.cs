using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Bloodstream;

/// <summary>
/// Defines the main blood type of person according to the AB0 system.
/// </summary>
[Serializable, NetSerializable]
public enum BloodType : byte
{
    O,
    A,
    B,
    AB,
}

/// <summary>
/// Defines the Rhesus factor of the blood.
/// </summary>
[Serializable, NetSerializable]
public enum BloodRhesusFactor : byte
{
    Positive,
    Negative,
}

/// <summary>
/// Represents a complete blood group, combining the AB0 type and the Rhesus factor.
/// </summary>
[Serializable, NetSerializable]
public struct BloodGroup
{
    [DataField]
    public BloodType Type = BloodType.O;

    [DataField]
    public BloodRhesusFactor RhesusFactor = BloodRhesusFactor.Negative;

    /// <summary>
    /// Initializes a new instance of the <see cref="BloodGroup"/> structure.
    /// </summary>
    public BloodGroup() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BloodGroup"/> structure.
    /// </summary>
    /// <param name="type">The AB0 type.</param>
    /// <param name="rhesusFactor">The Rhesus factor.</param>
    public BloodGroup(BloodType type, BloodRhesusFactor rhesusFactor)
    {
        Type = type;
        RhesusFactor = rhesusFactor;
    }

    /// <summary>
    /// Returns the standard string representation of the blood group (e.g., "+A", "-O").
    /// </summary>
    /// <returns>The string representation of the blood group.</returns>
    public override string ToString()
    {
        var type = Type.ToString();
        var rhesusFactor = RhesusFactor == BloodRhesusFactor.Positive ? "+" : "-";

        return $"{rhesusFactor}{type}";
    }

    public bool Equals(BloodGroup other) => other.Type == Type && other.RhesusFactor == RhesusFactor;

    public override bool Equals(object? obj) => obj is BloodGroup other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Type, RhesusFactor);

    public static bool operator ==(BloodGroup left, BloodGroup right) => left.Equals(right);

    public static bool operator !=(BloodGroup left, BloodGroup right) => !(left == right);
}
