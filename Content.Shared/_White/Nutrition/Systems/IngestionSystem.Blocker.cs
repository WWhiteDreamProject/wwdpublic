using Content.Shared._White.Nutrition.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;

namespace Content.Shared._White.Nutrition.Systems;

public sealed partial class IngestionSystem
{
    private void InitializeBlocker()
    {
        SubscribeLocalEvent<IngestionBlockerComponent, ItemMaskToggledEvent>(OnItemMaskToggled); // TODO: I hate ItemMaskToggledEvent, we should use ItemToggledEvent here. ItemToggle refactor btw.
        SubscribeLocalEvent<IngestionBlockerComponent, InventoryRelayedEvent<AttemptIngestedEvent>>(OnAttemptIngested);
    }

    #region Event Handling

    private void OnItemMaskToggled(Entity<IngestionBlockerComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.IsToggled;
    }

    private void OnAttemptIngested(Entity<IngestionBlockerComponent> ent, ref InventoryRelayedEvent<AttemptIngestedEvent> args)
    {
        if (args.Args.Cancelled || !ent.Comp.Enabled)
            return;

        args.Args.Cancelled = true;
        args.Args.Popup = Loc.GetString("ingestion-attempt-ingested-blocker", ("entity", ent));
    }

    #endregion
}
