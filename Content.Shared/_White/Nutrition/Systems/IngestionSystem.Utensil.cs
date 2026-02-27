using Content.Shared._White.Nutrition.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools.EntitySystems;

namespace Content.Shared._White.Nutrition.Systems;

public sealed partial class IngestionSystem
{
    private void InitializeUtensil()
    {
        SubscribeLocalEvent<UtensilComponent, AfterInteractEvent>(OnAfterInteract, after: new[] { typeof(ToolOpenableSystem) });
    }

    #region Event Handling

    private void OnAfterInteract(Entity<UtensilComponent> ent, ref AfterInteractEvent ev)
    {
        if (ev.Handled || ev.Target == null || !ev.CanReach)
            return;

        ev.Handled = TryUseUtensil(ent, ev.Target.Value, ev.User);
    }

    #endregion

    #region Public API

    private bool TryUseUtensil(Entity<UtensilComponent> ent, Entity<IngestibleComponent?> target, EntityUid user)
    {
        var ev = new GetUtensilsEvent();
        RaiseLocalEvent(target, ref ev);

        //Prevents food usage with a wrong utensil
        if (ev.Types != UtensilType.None && (ev.Types & utensil.Comp.Types) == 0)
        {
            _popup.PopupClient(Loc.GetString("ingestion-try-use-wrong-utensil", ("verb", GetEdibleVerb(target)), ("food", target), ("utensil", utensil.Owner)), user, user);
            return true;
        }

        if (!_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return true;

        return TryIngest(user, user, target);
    }

    #endregion
}
