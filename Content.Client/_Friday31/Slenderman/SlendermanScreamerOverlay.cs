using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._Friday31.Slenderman;

public sealed class SlendermanScreamerOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private ISawmill _sawmill = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private TimeSpan _startTime;
    private TimeSpan _endTime;
    private Texture? _slendermanTexture;
    private bool _isActive;

    public SlendermanScreamerOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = Logger.GetSawmill("slenderman.screamer");
        ZIndex = 200;
    }

    public void Show(float duration)
    {
        _startTime = _timing.CurTime;
        _endTime = _startTime + TimeSpan.FromSeconds(duration);
        _isActive = true;

        if (_slendermanTexture == null)
        {
            try
            {
                var rsi = _resourceCache.GetResource<RSIResource>("/Textures/_Friday31/Mobs/slenderman.rsi").RSI;
                if (rsi.TryGetState("slenderman", out var state))
                {
                    _slendermanTexture = state.GetFrame(RsiDirection.South, 0);
                    _sawmill.Debug("Texture loaded successfully!");
                }
                else
                {
                    _sawmill.Error("Failed to get 'slenderman' state from RSI!");
                }
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error loading texture: {ex.Message}");
            }
        }
    }

    public bool IsActive()
    {
        if (!_isActive)
            return false;

        if (_timing.CurTime >= _endTime)
        {
            _isActive = false;
            return false;
        }

        return true;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!IsActive())
            return false;

        if (_slendermanTexture == null)
        {
            _sawmill.Warning("Texture is null!");
            return false;
        }

        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_slendermanTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldAABB;

        var textureSize = _slendermanTexture.Size;
        var cropHeight = textureSize.Y * 0.44f;
        var sourceRect = new UIBox2(0, 0, textureSize.X, cropHeight);
        worldHandle.DrawTextureRectRegion(_slendermanTexture, viewport, null, sourceRect);
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        _slendermanTexture = null;
    }
}
