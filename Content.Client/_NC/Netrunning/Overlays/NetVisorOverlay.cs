using Content.Shared._NC.Netrunning.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Inventory;

namespace Content.Client._NC.Netrunning.Overlays;

public sealed class NetVisorOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _playerManager;
    private readonly InventorySystem _inventory;
    private readonly Robust.Shared.Timing.IGameTiming _gameTiming;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public NetVisorOverlay(IEntityManager entManager, IPlayerManager playerManager, InventorySystem inventory, Robust.Shared.Timing.IGameTiming gameTiming)
    {
        _entManager = entManager;
        _playerManager = playerManager;
        _inventory = inventory;
        _gameTiming = gameTiming;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalEntity;
        if (player == null) return;

        // Check if player has NetVisor equipped (eyes)
        // Or if they have the component directly (cybereyes)
        bool hasVisor = _entManager.HasComponent<NetVisorComponent>(player);

        if (!hasVisor && _inventory.TryGetSlotEntity(player.Value, "eyes", out var eyesUid))
        {
            if (_entManager.HasComponent<NetVisorComponent>(eyesUid))
                hasVisor = true;
        }

        if (!hasVisor) return;

        var handle = args.WorldHandle;
        var query = _entManager.EntityQueryEnumerator<CyberdeckComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var deck, out var xform))
        {
            if (deck.ActiveTarget == null) continue;

            var target = deck.ActiveTarget.Value;
            if (!_entManager.TryGetComponent<TransformComponent>(target, out var targetXform))
                continue;

            if (xform.MapID != args.MapId || targetXform.MapID != args.MapId)
                continue;

            // Draw Beam
            var startPos = _entManager.System<SharedTransformSystem>().GetWorldPosition(xform);
            var endPos = _entManager.System<SharedTransformSystem>().GetWorldPosition(targetXform);

            // Pulse effect
            float pulse = (float) Math.Sin(_gameTiming.RealTime.TotalSeconds * 5f) * 0.2f + 0.8f;
            var color = deck.BeamColor;
            color.A = pulse;

            handle.DrawLine(startPos, endPos, color);

            // Draw small circles at ends
            handle.DrawCircle(endPos, 0.2f, color);
        }
    }
}
