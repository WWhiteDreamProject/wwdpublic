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

    // Shader parameters
    private const float ScanlineIntensity = 0.15f;
    private const float Distortion = 0.008f;
    private const float TimeCoefficient = 0.0f; // Static scanlines

    // Time for animation
    private float _currentTime;

    // Effect parameters
    private float _impactDarkness = 0.0f;
    private float _healthPercentage = 1.0f;
    private float _glitchIntensity = 0.0f;

    // Temporary glitch effect parameters
    private float _temporaryGlitchIntensity = 0.0f;
    private float _temporaryGlitchDuration = 0.0f;
    private float _temporaryGlitchTimer = 0.0f;
    private bool _hasTemporaryGlitch = false;

    public CRTVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _crtVisionShader = _prototypeManager.Index<ShaderPrototype>("CRTVision").Instance().Duplicate();
        _crtVisionShader.SetParameter("SCANLINE_INTENSITY", ScanlineIntensity);
        _crtVisionShader.SetParameter("DISTORTION", Distortion);
        _crtVisionShader.SetParameter("TIME_COEFFICIENT", TimeCoefficient);
        _crtVisionShader.SetParameter("IMPACT_DARKNESS", _impactDarkness);
        _crtVisionShader.SetParameter("GLITCH_INTENSITY", _glitchIntensity);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.HasComponent<CRTVisionComponent>(player))
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        _currentTime += args.DeltaSeconds;

        // Update glitch intensity
        UpdateGlitchIntensity();

        // Process temporary glitch effect
        if (_hasTemporaryGlitch)
        {
            _temporaryGlitchTimer += args.DeltaSeconds;

            if (_temporaryGlitchTimer >= _temporaryGlitchDuration)
            {
                _temporaryGlitchTimer = 0.0f;
                _hasTemporaryGlitch = false;
                _temporaryGlitchIntensity = 0.0f;
            }
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _crtVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _crtVisionShader.SetParameter("TIME", _currentTime);
        _crtVisionShader.SetParameter("IMPACT_DARKNESS", _impactDarkness);

        // Apply temporary glitch effect if active
        float effectiveGlitchIntensity = _glitchIntensity;
        if (_hasTemporaryGlitch)
        {
            float remainingFactor = 1.0f - (_temporaryGlitchTimer / _temporaryGlitchDuration);
            float tempIntensity = _temporaryGlitchIntensity * remainingFactor;
            effectiveGlitchIntensity = Math.Max(effectiveGlitchIntensity, tempIntensity);
        }

        _crtVisionShader.SetParameter("GLITCH_INTENSITY", effectiveGlitchIntensity);

        var screenHandle = args.ScreenHandle;
        var viewport = args.ViewportBounds;
        screenHandle.UseShader(_crtVisionShader);
        screenHandle.DrawRect(viewport, Color.White);
        screenHandle.UseShader(null);
    }

    /// <summary>
    /// Sets the darkness intensity when taking damage
    /// </summary>
    public void SetImpactDarkness(float darkness)
    {
        _impactDarkness = darkness;
    }

    /// <summary>
    /// Sets the health percentage to determine glitch effect intensity
    /// </summary>
    public void SetHealthPercentage(float percentage)
    {
        _healthPercentage = percentage;
    }

    /// <summary>
    /// Updates glitch effect intensity based on current health
    /// </summary>
    private void UpdateGlitchIntensity()
    {
        _glitchIntensity = 0.0f;

        // Glitch effects appear only when health is below 70%
        if (_healthPercentage < 0.7f)
        {
            // Normalize scale from 0.7 to 0.0 into range from 0.0 to 1.0
            float normalizedHealth = 1.0f - (_healthPercentage / 0.7f);

            // Quadratic function for smooth effect increase
            _glitchIntensity = normalizedHealth * normalizedHealth * 0.5f;

            // Add oscillation for natural effect
            _glitchIntensity += (float)Math.Sin(_currentTime * 2.5f) * 0.03f;

            // Limit maximum intensity
            _glitchIntensity = Math.Min(_glitchIntensity, 0.75f);
        }

        // At low charge (below 20%) enhance effects
        if (_healthPercentage < 0.2f)
        {
            float lowChargeFactor = 1.0f - (_healthPercentage / 0.2f);
            float pulsation = (float)Math.Sin(_currentTime * 3.0f) * 0.08f;
            _glitchIntensity = Math.Max(_glitchIntensity, lowChargeFactor * 0.6f + pulsation);
        }
    }

    /// <summary>
    /// Sets temporary glitch effect when taking damage
    /// </summary>
    public void SetTemporaryGlitchEffect(float intensity, float duration)
    {
        _temporaryGlitchIntensity = intensity;
        _temporaryGlitchDuration = duration;
        _temporaryGlitchTimer = 0.0f;
        _hasTemporaryGlitch = true;
    }
}
