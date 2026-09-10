using System.Numerics;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Shared._White.Other;

public static partial class Crutches
{
    /// <summary>
    /// Calculates an intercept position for the given parameters. Does not account for shooter's velocity since the current projectile code doesn't either.
    /// </summary>
    /// <remarks>
    /// This method treats both bodies as volumeless points.
    /// </remarks>
    /// <param name="target">Target entity</param>
    /// <param name="shooter">Shooter entity</param>
    /// <param name="interceptWorldPosition">The resulting intercept position. If not found, defaults to <see cref="Vector2.Zero"/></param>
    /// <param name="maxLeadingDistance">How far the intercept position can be from the target body starting position. Set to non-positive to disable.</param>
    /// <param name="maxLeadingTime">How far into the future can the collision occur, in seconds. Set to non-positive to disable.</param>
    /// <returns>True if an intercept position is found; false otherwise. Will also return false if a required component is not found on one of the entities.</returns>
    public static bool FindInterceptionPoint(Entity<TransformComponent?, PhysicsComponent?> target, Entity<TransformComponent?, GunComponent?> shooter, out Vector2 interceptWorldPosition, float maxLeadingDistance = 1000f, float maxLeadingTime = 30f)
    {
        if (!Resolve(target, ref target.Comp1) || ! Resolve(target, ref target.Comp2) ||
            !Resolve(shooter, ref shooter.Comp1) || ! Resolve(shooter, ref shooter.Comp2) ||
            target.Comp1.MapID != shooter.Comp1.MapID)
        {
            interceptWorldPosition = Vector2.Zero;
            return false;
        }
        
        return FindInterceptionPoint(target.Comp1.WorldPosition,
                                     target.Comp2.LinearVelocity,
                                     shooter.Comp1.WorldPosition,
                                     shooter.Comp2.ProjectileSpeedModified,
                                     out interceptWorldPosition,
                                     maxLeadingDistance,
                                     maxLeadingTime);

    }

    /// <summary>
    /// Calculates an intercept position for the given parameters. Does not account for shooter's velocity since the current projectile code doesn't either.
    /// </summary>
    /// <remarks>
    /// This method treats both bodies as volumeless points.
    /// </remarks>
    /// <param name="targetBodyPosition">Target body's starting position</param>
    /// <param name="targetBodyVelocity">Target body's velocity</param>
    /// <param name="interceptorStartingPos">Interceptor body's starting position</param>
    /// <param name="interceptorSpeed">Interceptor body's speed</param>
    /// <param name="maxLeadingDistance">How far the intercept position can be from the target body starting position. Set to non-positive to disable.</param>
    /// <param name="maxLeadingTime">How far into the future can the collision occur, in seconds. Set to non-positive to disable.</param>
    /// <returns>The resulting intercept position. If not found, returns null.</returns>
    public static Vector2? FindInterceptionPoint(Vector2 targetBodyPosition,
                                                 Vector2 targetBodyVelocity,
                                                 Vector2 interceptorStartingPos,
                                                 float interceptorSpeed,
                                                 float maxLeadingDistance = 1000f,
                                                 float maxLeadingTime = 30f)
    {
        bool result = FindInterceptionPoint(targetBodyPosition, targetBodyVelocity, interceptorStartingPos, interceptorSpeed, out var interceptPosition, maxLeadingDistance, maxLeadingTime);
        return result ? interceptPosition : null;
    }

    /// <summary>
    /// Calculates an intercept position for the given parameters. Does not account for shooter's velocity since the current projectile code doesn't either.
    /// </summary>
    /// <remarks>
    /// This method treats both bodies as volumeless points.
    /// </remarks>
    /// <param name="targetBodyPosition">Target body's starting position</param>
    /// <param name="targetBodyVelocity">Target body's velocity</param>
    /// <param name="interceptorStartingPos">Interceptor body's starting position</param>
    /// <param name="interceptorSpeed">Interceptor body's speed</param>
    /// <param name="interceptPosition">The resulting intercept position. If not found, defaults to <see cref="Vector2.Zero"/></param>
    /// <param name="maxLeadingDistance">How far the intercept position can be from the target body starting position. Set to non-positive to disable.</param>
    /// <param name="maxLeadingTime">How far into the future can the collision occur, in seconds. Set to non-positive to disable.</param>
    /// <returns>True if an intercept position is found; false otherwise.</returns>
    public static bool FindInterceptionPoint(Vector2 targetBodyPosition,
                                             Vector2 targetBodyVelocity,
                                             Vector2 interceptorStartingPos,
                                             float interceptorSpeed,
                                             out Vector2 interceptPosition,
                                             float maxLeadingDistance = 1000f,
                                             float maxLeadingTime = 30f)
    {
        interceptPosition = Vector2.Zero;

        // using coordinate system where (0,0) is the interceptor starting pos makes math easier
        // first body starting position (second body position (p2) is assumed zero)
        var p = targetBodyPosition - interceptorStartingPos;
        // first body velocity
        var v = targetBodyVelocity;
        // second body speed 
        var u = interceptorSpeed;

        // TODO: consider solving quartic equation to also account for target acceleration
        //       not hard by any means, just an annoyingly large amount of typing 

        // we solve  | p + v*t | = u*t  for t
        // sqrt( (v.x + v.x*t)^2 + (v.y+v.y*t)^2 ) = u*t
        // (v.x + v.x*t)^2 + (v.y+v.y*t)^2 = (u*t)^2
        // ...
        // (v.x^2+v.y^2-u^2)*t^2 + 2t*(v.x*v.x+v.y*v.y) + (v.x^2 + v.y^2) = 0
        var a = v.X * v.X + v.Y * v.Y - u * u;
        var b = 2 * (p.X * v.X + p.Y * v.Y);
        var c = p.X * p.X + p.Y * p.Y;

        float t;
        if (a != 0)
        {
            var d = b * b - 4 * a * c;
            if (d < 0) // since shooting into parallel dimensions is prohibited, return false
                return false;

            // if a is positive, we check the -D root first
            // if it's negative, we check the +D root first
            d = MathF.Sqrt(d) * MathF.Sign(a);

            t = (-b - d) / (2 * a);
            if (t < 0)
            {
                t = (-b + d) / (2 * a);
                if (t < 0)
                    return false;
            }
        }
        else // bt + c = 0
        {
            t = -c / b;
            if (t < 0)
                return false;
        }

        if (maxLeadingDistance > 0 && t * t * v.LengthSquared() > maxLeadingDistance * maxLeadingDistance ||
            maxLeadingTime > 0 && t > maxLeadingTime)
            return false;

        interceptPosition = targetBodyPosition + targetBodyVelocity * t;
        return true;
    }
}