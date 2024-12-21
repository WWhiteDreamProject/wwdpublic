using Content.Shared._White.Plumbing;
using Content.Shared.Wires;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.Plumbing;

public sealed class PlumbingPipeVisualiserSystem : VisualizerSystem<PlumbingPipeVisComponent>
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlumbingPipeVisComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnAppearanceChange(EntityUid uid, PlumbingPipeVisComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        //if (!args.Sprite.Visible)
        //{
        //    // This entity is probably below a floor and is not even visible to the user -> don't bother updating sprite data.
        //    // Note that if the subfloor visuals change, then another AppearanceChangeEvent will get triggered.
        //    return;
        //}

        if (!AppearanceSystem.TryGetData<WireVisDirFlags>(uid, WireVisVisuals.ConnectedMask, out var mask, args.Component))
            mask = WireVisDirFlags.None;

        args.Sprite.LayerSetState(0, $"pipe_{(int)mask}");
    }
}
