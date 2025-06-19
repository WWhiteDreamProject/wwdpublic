using Content.Shared._White.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._White.Overlays;

// ReSharper disable once InconsistentNaming
public sealed class CRTVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly ShaderInstance _crtVisionShader;

    // Effect constants
    private const float GlitchHealthThreshold = 0.7f;
    private const float LowHealthThreshold = 0.2f;
    private const float GlitchIntensityMultiplier = 0.5f;
    private const float GlitchOscillationFrequency = 2.5f;
    private const float GlitchOscillationAmplitude = 0.03f;
    private const float MaxGlitchIntensity = 0.75f;
    private const float LowHealthPulsationFrequency = 3.0f;
    private const float LowHealthPulsationAmplitude = 0.08f;
    private const float LowHealthIntensityFactor = 0.6f;

    // Shader parameters
    private const float ScanlineIntensity = 0.15f;
    private const float Distortion = 0.008f;

    // Time for animation
    private float _currentTime;

    // Effect parameters
    private float _healthPercentage = 1.0f;
    private float _glitchIntensity;

    // Temporary glitch effect parameters
    private float _temporaryGlitchIntensity;
    private float _temporaryGlitchDuration;
    private float _temporaryGlitchTimer;
    private bool _hasTemporaryGlitch;

    public CRTVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = -100;
        _crtVisionShader = _prototypeManager.Index<ShaderPrototype>("CRTVision").Instance().Duplicate();
        _crtVisionShader.SetParameter("SCANLINE_INTENSITY", ScanlineIntensity);
        _crtVisionShader.SetParameter("DISTORTION", Distortion);
        _crtVisionShader.SetParameter("GLITCH_INTENSITY", _glitchIntensity);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true, } player
            || !_entityManager.HasComponent<CRTVisionOverlayComponent>(player))
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

        // Apply temporary glitch effect if active
        var effectiveGlitchIntensity = _glitchIntensity;
        if (_hasTemporaryGlitch && _temporaryGlitchDuration > 0f)
        {
            var remainingFactor = 1.0f - (_temporaryGlitchTimer / _temporaryGlitchDuration);
            var tempIntensity = _temporaryGlitchIntensity * remainingFactor;
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

        // Glitch effects appear only when health is below a threshold
        if (_healthPercentage < GlitchHealthThreshold)
        {
            // Normalize scale from threshold to 0.0 into range from 0.0 to 1.0
            var normalizedHealth = 1.0f - (_healthPercentage / GlitchHealthThreshold);

            // Quadratic function for smooth effect increase
            _glitchIntensity = normalizedHealth * normalizedHealth * GlitchIntensityMultiplier;

            // Add oscillation for natural effect
            _glitchIntensity += (float)Math.Sin(_currentTime * GlitchOscillationFrequency) * GlitchOscillationAmplitude;

            // Limit maximum intensity
            _glitchIntensity = Math.Min(_glitchIntensity, MaxGlitchIntensity);
        }

        // At low charge enhance effects
        if (_healthPercentage < LowHealthThreshold)
        {
            var lowChargeFactor = 1.0f - (_healthPercentage / LowHealthThreshold);
            var pulsation = (float)Math.Sin(_currentTime * LowHealthPulsationFrequency) * LowHealthPulsationAmplitude;
            _glitchIntensity = Math.Max(_glitchIntensity, lowChargeFactor * LowHealthIntensityFactor + pulsation);
        }
        _glitchIntensity = Math.Max(0f, _glitchIntensity);
    }

    /// <summary>
    /// Sets temporary glitch effect when taking damage
    /// </summary>
    public void SetTemporaryGlitchEffect(float intensity, float duration)
    {
        // Validate parameters
        intensity = Math.Max(0f, intensity);
        duration = Math.Max(0f, duration);

        _temporaryGlitchIntensity = intensity;
        _temporaryGlitchDuration = duration;
        _temporaryGlitchTimer = 0.0f;
        _hasTemporaryGlitch = intensity > 0f && duration > 0f;
    }
}
