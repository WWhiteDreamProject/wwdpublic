using Content.Shared._White.Mobs;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Mobs.Overlays;

public sealed class MobStateOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override bool RequestScreenTexture => false;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private float _level;
    private MobStateOverlayData _data = new();

    private readonly ShaderInstance _shader;

    private static readonly ProtoId<ShaderPrototype> Shader = "Vignette";

    public MobStateOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototype.Index(Shader).InstanceUnique();
    }

    public void Clear()
    {
        _level = 0;
    }

    public void SetData(MobStateOverlayData data)
    {
        _data = data;
    }

    public void SetLevel(float level)
    {
        _level = level;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_level == 0)
            return;

        var handle = args.WorldHandle;
        var width = args.ViewportBounds.Width;

        var color = new Vector3(_data.Color.R, _data.Color.G, _data.Color.B);

        var innerMax = _data.InnerMax * width;
        var innerMin = _data.InnerMin * width;
        var outerMax = _data.OuterMax * width;
        var outerMin = _data.OuterMin * width;

        var innerMinRadius = innerMax - _level * (innerMax - innerMin);
        var innerMaxRadius = innerMinRadius + _data.InnerPulse * width;

        var outerMinRadius = outerMax - _level * (outerMax - outerMin);
        var outerMaxRadius = outerMinRadius + _data.OuterPulse * width;

        _shader.SetParameter("innerAlpha", _data.InnerAlpha);
        _shader.SetParameter("innerMax", innerMinRadius);
        _shader.SetParameter("innerMin", innerMaxRadius);
        _shader.SetParameter("outerAlpha", _data.OuterAlpha);
        _shader.SetParameter("outerMax", outerMinRadius);
        _shader.SetParameter("outerMin", outerMaxRadius);
        _shader.SetParameter("rate", _data.Rate);
        _shader.SetParameter("color", color);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
