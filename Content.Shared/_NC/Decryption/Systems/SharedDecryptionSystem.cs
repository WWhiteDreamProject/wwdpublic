using Content.Shared._NC.Decryption.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._NC.Decryption.Systems;

// Shared slot lifecycle for the decryption terminal.
public sealed class SharedDecryptionSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DecryptionTerminalComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DecryptionTerminalComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, DecryptionTerminalComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, DecryptionTerminalComponent.DataSlotId, component.DataSlot);
    }

    private void OnComponentRemove(EntityUid uid, DecryptionTerminalComponent component, ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(uid, component.DataSlot);
    }
}
