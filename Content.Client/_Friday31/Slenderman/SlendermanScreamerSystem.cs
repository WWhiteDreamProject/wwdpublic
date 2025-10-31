using Content.Shared._Friday31.Slenderman;
using Robust.Client.Graphics;
using Robust.Shared.Log;

namespace Content.Client._Friday31.Slenderman;

public sealed class SlendermanScreamerSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private SlendermanScreamerOverlay? _overlay;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("slenderman.screamer");
        SubscribeNetworkEvent<SlendermanScreamerEvent>(OnScreamerEvent);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_overlay != null)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _overlay = null;
        }
    }

    private void OnScreamerEvent(SlendermanScreamerEvent ev)
    {
        _sawmill.Info($"Received screamer event! Duration: {ev.Duration}");

        if (_overlay == null)
        {
            _overlay = new SlendermanScreamerOverlay();
            _sawmill.Debug("Created new overlay instance");
        }

        _overlay.Show(ev.Duration);
        _sawmill.Debug($"Overlay.Show() called, IsActive: {_overlay.IsActive()}");

        if (!_overlayManager.HasOverlay<SlendermanScreamerOverlay>())
        {
            _overlayManager.AddOverlay(_overlay);
            _sawmill.Info("Overlay added to manager");
        }
        else
        {
            _sawmill.Debug("Overlay already in manager");
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_overlay != null && !_overlay.IsActive())
        {
            if (_overlayManager.HasOverlay<SlendermanScreamerOverlay>())
            {
                _overlayManager.RemoveOverlay(_overlay);
            }
        }
    }
}
