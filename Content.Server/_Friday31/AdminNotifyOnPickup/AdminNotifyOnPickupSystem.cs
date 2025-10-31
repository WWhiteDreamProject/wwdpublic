using Content.Server.Chat.Managers;
using Content.Shared._Friday31.AdminNotifyOnPickup;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;

namespace Content.Server._Friday31.AdminNotifyOnPickup;

public sealed class AdminNotifyOnPickupSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdminNotifyOnPickupComponent, ItemPickedUpEvent>(OnItemPickedUp);
    }

    private void OnItemPickedUp(EntityUid uid, AdminNotifyOnPickupComponent component, ItemPickedUpEvent args)
    {
        if (string.IsNullOrEmpty(component.Message))
            return;

        var playerName = Identity.Name(args.User, EntityManager);
        var itemName = MetaData(uid).EntityName;

        var fullMessage = $"Игрок {playerName} {component.Message}";

        _chatManager.SendAdminAnnouncement(fullMessage, null, null);
    }
}
