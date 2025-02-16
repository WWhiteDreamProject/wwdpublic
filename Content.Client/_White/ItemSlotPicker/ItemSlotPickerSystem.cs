using Content.Shared._White.ItemSlotPicker;
using Content.Shared.Interaction;
using Content.Shared._White.ItemSlotPicker.UI;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.ItemSlotPicker;

public sealed class ItemSlotPickerSystem : SharedItemSlotPickerSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemSlotPickerComponent, EntInsertedIntoContainerMessage>(EntInserted);
        SubscribeLocalEvent<ItemSlotPickerComponent, EntRemovedFromContainerMessage>(EntRemoved);
    }

    private void EntInserted(EntityUid uid, ItemSlotPickerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted ||
            _player.LocalEntity is not EntityUid player ||
            !_ui.IsUiOpen(uid, ItemSlotPickerKey.Key, player))
            return;
        var msg = new ItemSlotPickerContentsChangedMessage();
        msg.Actor = player;
        _ui.RaiseUiMessage(uid, ItemSlotPickerKey.Key, msg);
    }

    private void EntRemoved(EntityUid uid, ItemSlotPickerComponent comp, EntRemovedFromContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted ||
            _player.LocalEntity is not EntityUid player ||
            !_ui.IsUiOpen(uid, ItemSlotPickerKey.Key, player))
            return;
        var msg = new ItemSlotPickerContentsChangedMessage();
        msg.Actor = player;
        _ui.RaiseUiMessage(uid, ItemSlotPickerKey.Key, msg);
    }

}
