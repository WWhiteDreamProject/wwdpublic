using Content.Server.Shuttles.Components;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Server.Popups;

namespace Content.Server._War.DiskTag;

public sealed class DiskTagSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DiskTagComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, DiskTagComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<ShuttleConsoleComponent>(target))
            return;

        // Проверяем, что диск имеет TagComponent
        if (!TryComp<TagComponent>(uid, out var diskTags))
            return;

        // Добавляем все теги диска к цели
        foreach (var tag in diskTags.Tags)
        {
            _tagSystem.AddTags(target, tag);
        }
        _popup.PopupEntity(Loc.GetString("disk-tag-inserted"), target, args.User);
        QueueDel(uid);
        args.Handled = true;
    }
}
