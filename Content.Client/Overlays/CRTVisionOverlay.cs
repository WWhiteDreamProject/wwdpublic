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

    // Параметр затемнения при ударе
    private float _impactDarkness = 0.0f;

    // Параметр для отслеживания здоровья (1.0 = полное здоровье, 0.0 = критическое состояние)
    private float _healthPercentage = 1.0f;

    // Параметры для глитч-эффектов
    private float _glitchIntensity = 0.0f;

    // Параметры для временного эффекта глитча при ударе
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

    // Update time for animation
    protected override void FrameUpdate(FrameEventArgs args)
    {
        _currentTime += args.DeltaSeconds;

        // Обновляем интенсивность глитчей на основе здоровья
        UpdateGlitchIntensity();

        // Обработка временного эффекта глитча
        if (_hasTemporaryGlitch)
        {
            _temporaryGlitchTimer += args.DeltaSeconds;

            if (_temporaryGlitchTimer >= _temporaryGlitchDuration)
            {
                // Время эффекта истекло
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

        // Применяем временный эффект глитча, если он активен
        float effectiveGlitchIntensity = _glitchIntensity;
        if (_hasTemporaryGlitch)
        {
            // Для удара - эффект начинается сильным и постепенно затухает
            float remainingFactor = 1.0f - (_temporaryGlitchTimer / _temporaryGlitchDuration);
            float tempIntensity = _temporaryGlitchIntensity * remainingFactor;

            // Берем максимум из базовой интенсивности и временной
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
    /// Устанавливает интенсивность затемнения при получении урона
    /// </summary>
    /// <param name="darkness">Значение от 0.0 до 1.0</param>
    public void SetImpactDarkness(float darkness)
    {
        _impactDarkness = darkness;
    }

    /// <summary>
    /// Устанавливает процент здоровья для определения интенсивности глитч-эффектов
    /// </summary>
    /// <param name="percentage">Значение от 0.0 (критическое) до 1.0 (полное здоровье)</param>
    public void SetHealthPercentage(float percentage)
    {
        _healthPercentage = percentage;
    }

    /// <summary>
    /// Обновляет интенсивность глитч-эффектов на основе текущего здоровья
    /// </summary>
    private void UpdateGlitchIntensity()
    {
        // При полном здоровье - никаких глитч эффектов
        _glitchIntensity = 0.0f;

        // Глитч эффекты появляются только когда здоровье ниже 70%
        if (_healthPercentage < 0.7f)
        {
            // Нормализуем шкалу от 0.7 до 0.0 в диапазон от 0.0 до 1.0
            float normalizedHealth = 1.0f - (_healthPercentage / 0.7f);

            // Более мягкая кривая для нарастания эффектов
            // Используем квадратичную функцию с меньшим коэффициентом
            _glitchIntensity = normalizedHealth * normalizedHealth * 0.5f;

            // Добавляем небольшие случайные колебания для более естественного эффекта
            _glitchIntensity += (float)Math.Sin(_currentTime * 2.5f) * 0.03f;

            // Ограничиваем максимальную интенсивность
            _glitchIntensity = Math.Min(_glitchIntensity, 0.75f);
        }

        // При очень низком заряде (ниже 20%) усиливаем эффекты глитчей
        if (_healthPercentage < 0.2f)
        {
            // Усиливаем глитч-эффекты при низком заряде
            float lowChargeFactor = 1.0f - (_healthPercentage / 0.2f);

            // Добавляем пульсацию для эффекта нестабильной работы
            float pulsation = (float)Math.Sin(_currentTime * 3.0f) * 0.08f;

            // Комбинируем базовую интенсивность с эффектами низкого заряда
            _glitchIntensity = Math.Max(_glitchIntensity, lowChargeFactor * 0.6f + pulsation);
        }
    }

    /// <summary>
    /// Устанавливает временный эффект глитча при получении урона
    /// </summary>
    /// <param name="intensity">Интенсивность эффекта (0.0 - 1.0)</param>
    /// <param name="duration">Продолжительность эффекта в секундах</param>
    public void SetTemporaryGlitchEffect(float intensity, float duration)
    {
        _temporaryGlitchIntensity = intensity;
        _temporaryGlitchDuration = duration;
        _temporaryGlitchTimer = 0.0f;
        _hasTemporaryGlitch = true;
    }
}
