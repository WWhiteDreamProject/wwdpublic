using Content.Shared._White;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.AntiParkinsons;

public static class PPCamHelper
{
    private static int roundFactor => EyeManager.PixelsPerMeter;

    public static Vector2 RoundXY(Vector2 vec) => new Vector2(MathF.Round(vec.X * roundFactor) / roundFactor, MathF.Round(vec.Y * roundFactor) / roundFactor);

    /// <summary>
    /// Translates world vector into local (to parent) vector, rounds it to a 1 over <see cref="EyeManager.PixelsPerMeter"/> and translates back to world space.
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="parentXform"></param>
    /// <returns></returns>
    public static Vector2 WorldPosPixelRoundToParent(Vector2 worldPos, EntityUid parent, SharedTransformSystem xformSystem)
    {
        var (_, _, mat, invmat) = xformSystem.GetWorldPositionRotationMatrixWithInv(parent);
        Vector2 localSpacePos = Vector2.Transform(worldPos, invmat);
        localSpacePos = RoundXY(localSpacePos);
        Vector2 worldRoundedPos = Vector2.Transform(localSpacePos, mat);
        return worldRoundedPos;
    }

    public static (Vector2 roundedWorldPos, Vector2 LocalSpaceDiff) WorldPosPixelRoundToParentWithDiff(Vector2 worldPos, EntityUid parent, SharedTransformSystem xformSystem)
    {
        var (_, _, mat, invmat) = xformSystem.GetWorldPositionRotationMatrixWithInv(parent);
        Vector2 localSpacePos = Vector2.Transform(worldPos, invmat);
        var roundedLocalSpacePos = RoundXY(localSpacePos);
        Vector2 worldRoundedPos = Vector2.Transform(localSpacePos, mat);
        return (worldRoundedPos, roundedLocalSpacePos - localSpacePos);
    }

    public static T CheckForChange<T>(T currentValue, T modifiedValue, T originalValue) where T : IEquatable<T>
    {
        // if this is false, this means that the value tracked was changed outside
        // of the engine's FrameUpdate loop, and this change should be preserved.
        if (currentValue.Equals(modifiedValue))
            return originalValue;
        return currentValue;
    }
}
