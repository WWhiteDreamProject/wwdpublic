using System.Numerics;
using System.Runtime.CompilerServices;

namespace Content.Shared._White.Maths;

public static class WhiteVector3
{
    /// <summary>
    /// Calculates the angle (in radians) between two vectors.
    /// </summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    /// <returns>Angle (in radians) between the vectors.</returns>
    /// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(Vector2 first, Vector2 second)
    {
        return MathF.Acos(Dot(first, second) / (first.Length() * second.Length()));
    }

    /// <summary>
    /// Calculates the angle (in radians) between two vectors.
    /// </summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    /// <returns>Angle (in radians) between the vectors.</returns>
    /// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(Vector3 first, Vector3 second)
    {
        return MathF.Acos(Dot(first, second) / (first.Length() * second.Length()));
    }

    /// <summary>
    /// Calculate the dot (scalar) product of two vectors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector2 first, Vector2 second)
    {
        return first.X * second.X + first.Y * second.Y;
    }

    /// <summary>
    /// Calculate the dot (scalar) product of two vectors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3 first, Vector3 second)
    {
        return first.X * second.X + first.Y * second.Y + first.Z * second.Z;
    }
}
