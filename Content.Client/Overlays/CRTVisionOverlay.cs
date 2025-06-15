using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Traits.Assorted.Components;
using Robust.Shared.Timing;

namespace Content.Client.Overlays;

public sealed class CRTVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    private readonly ShaderInstance _crtVisionShader;

    // Parameters for the CRT shader
    private const float ScanlineIntensity = 0.15f;
    private const float Distortion = 0.008f;
    private const float TimeCoefficient = 0.0f; // Статичные сканлайны без движения

    // Store current time for animation
    private float _currentTime;

    public CRTVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _crtVisionShader = _prototypeManager.Index<ShaderPrototype>("CRTVision").Instance().Duplicate();
        _crtVisionShader.SetParameter("SCANLINE_INTENSITY", ScanlineIntensity);
        _crtVisionShader.SetParameter("DISTORTION", Distortion);
        _crtVisionShader.SetParameter("TIME_COEFFICIENT", TimeCoefficient);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.HasComponent<CRTVisionComponent>(player))
            return false;

        return base.BeforeDraw(in args);
    }

    // Update time for animation
    protected override void FrameUpdate(FrameEventArgs args)
    {
        _currentTime += args.DeltaSeconds;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _crtVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _crtVisionShader.SetParameter("TIME", _currentTime);

        var screenHandle = args.ScreenHandle;
        var viewport = args.ViewportBounds;
        screenHandle.UseShader(_crtVisionShader);
        screenHandle.DrawRect(viewport, Color.White);
        screenHandle.UseShader(null);
    }
}
