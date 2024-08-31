using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._White.Wield;

public sealed class ToggleableWieldedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableWieldedComponent, ItemToggleActivateAttemptEvent>(AttemptActivate);
    }

    private void AttemptActivate(Entity<ToggleableWieldedComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (TryComp(ent, out WieldableComponent? wieldable) && !wieldable.Wielded)
            args.Cancelled = true;
    }
}
