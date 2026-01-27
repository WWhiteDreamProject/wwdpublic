using Content.Client._NC.Netrunning.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.Inventory;

namespace Content.Client._NC.Netrunning.Systems;

public sealed class NetVisorSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly Robust.Shared.Timing.IGameTiming _gameTiming = default!;

    private NetVisorOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new NetVisorOverlay(EntityManager, _playerManager, _inventory, _gameTiming);
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }
}
