using Content.Client.Resources;
using Content.Shared._White.Pain.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._White.Pain.Overlays;

public sealed class PainOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private float _pain;
    private PainOverlayComponent? _comp;

    private readonly Texture _texture;

    private static readonly ResPath BlinkPath = new("/Textures/_White/Interface/Overlays/Pain/blink.png");

    public override bool RequestScreenTexture => false;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public PainOverlay()
    {
        IoCManager.InjectDependencies(this);

        _texture = _resourceCache.GetTexture(BlinkPath);
    }

    public void Clear()
    {
        _pain = 0;
        _comp = null;
    }

    public void AddPain(float pain)
    {
        if (_comp == null)
            return;

        _pain = Math.Clamp(_pain + pain, _comp.MinPain, _comp.MaxPain);
    }

    public void SetComp(PainOverlayComponent comp)
    {
        _comp = comp;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_pain <= 0)
            return;

        _pain -= args.DeltaSeconds * 30;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_comp == null)
            return;

        if (_pain <= 0)
            return;

        var handle = args.WorldHandle;

        var alpha = Math.Clamp(_pain / _comp.MaxEffect, 0, 1);
        handle.DrawRect(args.WorldBounds, new Color(_comp.Color.RGBA).WithAlpha(alpha));
        handle.DrawTextureRect(_texture, args.WorldBounds, Color.White.WithAlpha(alpha));
    }
}
