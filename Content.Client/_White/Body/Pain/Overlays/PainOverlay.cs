using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._White.Body.Pain.Overlays;

public sealed class PainOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _critCircleShader;
    private readonly ShaderInstance _painCircleShader;
    private readonly ShaderInstance _painBlinkShader;

    public MobState State = MobState.Alive;

    /// <summary>
    /// Handles the red pulsing overlay
    /// </summary>
    public float PainLevel = 0f;

    private float _oldPainLevel;

    /// <summary>
    /// Handles the white overlay when crit.
    /// </summary>
    public float CritLevel = 0f;

    private float _oldCritLevel;

    /// <summary>
    /// The percent to tint the screen by on a scale of 0-1.
    /// </summary>
    public float PainBlink;

    public float DeadLevel = 1f;

    private Texture _painTexture;

    public PainOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = -5;

        _critCircleShader = _prototype.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        _painCircleShader = _prototype.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        _painBlinkShader = _prototype.Index<ShaderPrototype>("PainBlink").InstanceUnique();

        _painTexture = _resourceCache.GetResource<TextureResource>("/Textures/_White/image.png").Texture;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_player.LocalEntity, out EyeComponent? eyeComp) || args.Viewport.Eye != eyeComp.Eye)
            return;

        var viewport = args.WorldAABB;
        var handle = args.WorldHandle;
        var distance = args.ViewportBounds.Width;

        var time = (float) _gameTiming.RealTime.TotalSeconds;
        var lastFrameTime = (float) _gameTiming.FrameTime.TotalSeconds;

        if (State != MobState.Dead)
        {
            DeadLevel = 1f;
        }
        else if (!MathHelper.CloseTo(0f, DeadLevel, 0.001f))
        {
            var diff = -DeadLevel;
            DeadLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            DeadLevel = 0f;
        }

        if (!MathHelper.CloseTo(_oldPainLevel, PainLevel, 0.001f))
        {
            var diff = PainLevel - _oldPainLevel;
            _oldPainLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldPainLevel = PainLevel;
        }

        if (!MathHelper.CloseTo(_oldCritLevel, CritLevel, 0.001f))
        {
            var diff = CritLevel - _oldCritLevel;
            _oldCritLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldCritLevel = CritLevel;
        }

        if (!MathHelper.CloseTo(0f, PainBlink, 0.001f))
        {
            var diff = -PainBlink / 2;
            PainBlink += GetDiff(diff, lastFrameTime);
        }
        else
        {
            PainBlink = 0f;
        }

        var level = PainBlink;
        if (level > 0 && ScreenTexture is not null)
        {
            _painBlinkShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _painBlinkShader.SetParameter("strength", PainBlink);
            _painBlinkShader.SetParameter("overlay_texture", _painTexture);

            handle.UseShader(_painBlinkShader);
            handle.DrawRect(viewport, Color.White);
        }

        level = _oldPainLevel;
        if (level > 0f && _oldCritLevel <= 0f)
        {
            var adjustedTime = time * 3f;
            var outerMaxLevel = 2.0f * distance;
            var outerMinLevel = 0.8f * distance;
            var innerMaxLevel = 0.6f * distance;
            var innerMinLevel = 0.2f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            var pulse = MathF.Max(0f, MathF.Sin(adjustedTime));

            _painCircleShader.SetParameter("time", pulse);
            _painCircleShader.SetParameter("color", new Vector3(1f, 0f, 0f));
            _painCircleShader.SetParameter("darknessAlphaOuter", 0.8f);

            _painCircleShader.SetParameter("outerCircleRadius", outerRadius);
            _painCircleShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            _painCircleShader.SetParameter("innerCircleRadius", innerRadius);
            _painCircleShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.02f * distance);

            handle.UseShader(_painCircleShader);
            handle.DrawRect(viewport, Color.White);
        }
        else
        {
            _oldPainLevel = PainLevel;
        }

        level = State != MobState.Dead ? _oldCritLevel : DeadLevel;
        if (level > 0f)
        {
            var outerMaxLevel = 2.0f * distance;
            var outerMinLevel = 1.0f * distance;
            var innerMaxLevel = 0.6f * distance;
            var innerMinLevel = 0.02f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            var pulse = MathF.Max(0f, MathF.Sin(time));

            _critCircleShader.SetParameter("time", pulse);
            _critCircleShader.SetParameter("color", new Vector3(1f, 1f, 1f));
            _critCircleShader.SetParameter("darknessAlphaOuter", 1.0f);

            _critCircleShader.SetParameter("innerCircleRadius", innerRadius);
            _critCircleShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.005f * distance);
            _critCircleShader.SetParameter("outerCircleRadius", outerRadius);
            _critCircleShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);

            handle.UseShader(_critCircleShader);
            handle.DrawRect(viewport, Color.White);
        }

        handle.UseShader(null);
    }

    private float GetDiff(float value, float lastFrameTime)
    {
        var adjustment = value * 5f * lastFrameTime;
        return value < 0f ? Math.Clamp(adjustment, value, -value) : Math.Clamp(adjustment, -value, value);
    }
}
