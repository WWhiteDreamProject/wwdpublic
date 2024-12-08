using Content.Shared.Nutrition.Components;
using Content.Shared.Clothing;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class IngestionBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IngestionBlockerComponent, ItemMaskToggledEvent>(OnBlockerMaskToggled);
    }

    private void OnBlockerMaskToggled(Entity<IngestionBlockerComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.IsToggled;
    }
}
