using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Overlays.Switchable;

public sealed class NightVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public bool IsActive = true;

    private readonly ProtoId<ShaderPrototype> _shaderProto = new("NightVision");
    private readonly ShaderInstance _shader;

    private Vector3 _tint = Vector3.One * 0.5f;
    private float _strength = 1;
    private float _noise = 0.5f;
    private Color _color = Color.Red;
    private float _pulseTime;

    private float _timeAccumulator;

    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(_shaderProto).InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        _timeAccumulator += args.DeltaSeconds;
        if (_timeAccumulator >= _pulseTime)
            _timeAccumulator = 0;
    }

    public void SetParams(Vector3 tint, float strength, float noise, Color color, float pulseTime)
    {
        _tint = tint;
        _strength = strength;
        _noise = noise;
        _color = color;
        _pulseTime = pulseTime;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || !IsActive)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tint", _tint);
        _shader.SetParameter("luminance_threshold", _strength);
        _shader.SetParameter("noise_amount", _noise);

        var worldHandle = args.WorldHandle;

        var alpha = _pulseTime <= 0f
            ? 1f
            : float.Lerp(1f, 0f, _timeAccumulator / _pulseTime);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, _color.WithAlpha(alpha));
        worldHandle.UseShader(null);
    }
}
