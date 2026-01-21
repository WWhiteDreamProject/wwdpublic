using Content.Shared._NC.Bank.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared._NC.Bank.ATM
{
    public sealed class SharedAtmSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AtmComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<AtmComponent, ComponentRemove>(OnComponentRemove);
        }

        private void OnComponentInit(EntityUid uid, AtmComponent component, ComponentInit args)
        {
            // Регистрируем слоты.
            // Теперь ItemSlotsSystem сам сделает всю работу (Eject/Insert).
            _itemSlotsSystem.AddItemSlot(uid, AtmComponent.IdSlotId, component.IdSlot);
            _itemSlotsSystem.AddItemSlot(uid, AtmComponent.CashSlotId, component.CashSlot);
        }

        private void OnComponentRemove(EntityUid uid, AtmComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.IdSlot);
            _itemSlotsSystem.RemoveItemSlot(uid, component.CashSlot);
        }
    }
}