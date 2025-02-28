using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Flash
{
    public abstract class SharedFlashSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FlashableComponent, ComponentGetState>(OnFlashableGetState);
        }

        private static void OnFlashableGetState(EntityUid uid, FlashableComponent component, ref ComponentGetState args)
        {
            args.State = new FlashableComponentState(component.Duration, component.LastFlash, component.EyeDamageChance, component.EyeDamage, component.DurationMultiplier);
        }
    }

    // WD EDIT START
    public sealed class FlashbangedEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public float MaxRange;

        public SlotFlags TargetSlots => SlotFlags.EARS | SlotFlags.HEAD;

        public FlashbangedEvent(float maxRange)
        {
            MaxRange = maxRange;
        }
    }
    // WD EDIT END
}
