using System;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._NC.Trade.Controls;

internal static class NcUiIconFit
{
    private const float ScaleMin = 0.5f;
    private const float ScaleMax = 6f;

    private static readonly Dictionary<(string ProtoId, int TargetPx, int PaddingPx, int Variant), float> Cache = new();

    public static void Fit(
        EntityPrototypeView view,
        SpriteSystem sprites,
        string protoId,
        int targetPx,
        int paddingPx = 2,
        float mul = 1f,
        int variant = 0)
    {
        if (string.IsNullOrWhiteSpace(protoId) || targetPx <= 0)
            return;

        var key = (protoId, targetPx, paddingPx, variant);
        if (Cache.TryGetValue(key, out var cached))
        {
            view.Scale = new(cached, cached);
            return;
        }

        var tex = sprites.GetPrototypeIcon(protoId).Default;
        if (tex == null)
            return;

        var size = tex.Size;
        var maxPx = Math.Max(size.X, size.Y);
        if (maxPx <= 0)
            return;

        var usable = Math.Max(1, targetPx - paddingPx);
        var scale = (float) usable / maxPx;

        scale *= mul;
        var min = variant == 1 ? 1f : ScaleMin;
        scale = MathF.Min(ScaleMax, MathF.Max(min, scale));
        Cache[key] = scale;
        view.Scale = new(scale, scale);
    }
}
