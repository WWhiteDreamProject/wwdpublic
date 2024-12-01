using Content.Shared.Flash;
using Content.Shared.Inventory;

namespace Content.Server._White.Flash;

public sealed class FlashSoundSuppressionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashSoundSuppressionComponent, InventoryRelayedEvent<FlashbangedEvent>>(OnFlashbanged);
    }

    private void OnFlashbanged(Entity<FlashSoundSuppressionComponent> ent, ref InventoryRelayedEvent<FlashbangedEvent> args)
    {
        args.Args.MaxRange = MathF.Min(args.Args.MaxRange, ent.Comp.MaxRange);
    }
}
