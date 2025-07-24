using Content.Client._White.Glasses.UI;
using Content.Shared._White.Glasses.Systems;
using Content.Shared._White.Glasses.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Security;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._White.Glasses.Systems;

public sealed class SecurityGlassesWantedStatusSystem : SharedSecurityGlassesWantedStatusSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeNetworkEvent<SecurityGlassesWantedStatusOpenEvent>(OnOpenRadialMenu);
        SubscribeLocalEvent<SecurityGlassesWantedStatusComponent, GotUnequippedEvent>(OnGlassesUnequipped);
    }
    
    private void OnOpenRadialMenu(SecurityGlassesWantedStatusOpenEvent ev)
    {
        var localPlayer = _playerManager.LocalSession?.AttachedEntity;
        if (localPlayer == null || !_entityManager.TryGetEntity(ev.User, out var userEntity) || userEntity != localPlayer)
            return;

        if (!_entityManager.TryGetEntity(ev.Target, out var targetEntity))
            return;
        
        var menu = new SecurityGlassesRadialMenu();
        
        Action<SecurityStatus, string?>? handler = null;
        handler = (status, reason) =>
        {
            var targetNet = _entityManager.GetNetEntity(targetEntity.Value);
            var userNet = _entityManager.GetNetEntity(userEntity.Value);

            RaiseNetworkEvent(new SecurityGlassesChangeStatusEvent(targetNet, userNet, (int)status, reason));
            
            if (handler != null)
                menu.OnStatusSelected -= handler;
            
            menu.Close();
        };
        menu.OnStatusSelected += handler;

        if (_entityManager.TryGetComponent<TransformComponent>(targetEntity.Value, out var transform))
        {
            var worldPos = transform.WorldPosition;
            var screenPos = _eyeManager.WorldToScreen(worldPos);
            menu.Open(screenPos);
        }
        else
        {
            menu.OpenCentered();
        }
        menu.MoveToFront();
    }
    
    private void OnGlassesUnequipped(EntityUid uid, SecurityGlassesWantedStatusComponent component, GotUnequippedEvent args)
    {
        if (args.SlotFlags.HasFlag(SlotFlags.EYES))
        {
            SecurityGlassesRadialMenu.GetCurrentMenu()?.Close();
        }
    }
} 