using Content.Shared._White.Nutrition.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Tools.EntitySystems;

namespace Content.Shared._White.Nutrition.Systems;

public abstract partial class SharedIngestionSystem
{
    private void InitializeUtensil()
    {
        SubscribeLocalEvent<UtensilComponent, AfterInteractEvent>(OnAfterInteract, after: new[] { typeof(ToolOpenableSystem) });
        SubscribeLocalEvent<UtensilComponent, GetUtensilsEvent>(OnGetUtensils);
    }

    #region Event Handling

    private void OnAfterInteract(Entity<UtensilComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryUseUtensil(ent, args.Target.Value, args.User);
    }

    private void OnGetUtensils(Entity<UtensilComponent> ent, ref GetUtensilsEvent args)
    {
        if (!args.Type.HasFlag(ent.Comp.Types))
            return;

        args.Utensils.Add(ent);
        args.Result |= ent.Comp.Types;
    }

    #endregion

    #region Public API

    private bool TryUseUtensil(Entity<UtensilComponent> ent, Entity<IngestibleComponent?> ingestible, EntityUid user)
    {
        if (!IngestibleQuery.Resolve(ingestible, ref ingestible.Comp, false))
            return false;

        if (ent.Comp.Types.HasFlag(ingestible.Comp.Utensil))
            return TryIngest(ingestible, user, user);

        var message = Loc.GetString("ingestion-try-use-wrong-utensil", ("verb", Loc.GetString(ingestible.Comp.Verb)), ("food", ingestible), ("utensil", Identity.Entity(ent, EntityManager)));
        _popup.PopupClient(message, user, user);
        return false;

    }

    #endregion
}
