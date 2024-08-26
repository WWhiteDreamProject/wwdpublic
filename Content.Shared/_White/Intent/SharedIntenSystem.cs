using Content.Shared.Actions;

namespace Content.Shared._White.Intent;

public sealed class SharedIntentsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IntentComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<IntentComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<IntentComponent, ToggleIntentEvent>(OnToggleIntent);
    }

    private void OnStartup(EntityUid uid, IntentComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.AddAction(uid, ref component.HelpActionEntity, component.HelpAction, component: comp);
        _action.AddAction(uid, ref component.DisarmActionEntity, component.DisarmAction, component: comp);
        _action.AddAction(uid, ref component.GrabActionEntity, component.GrabAction, component: comp);
        _action.AddAction(uid, ref component.HarmActionEntity, component.HarmAction, component: comp);

        UpdateActions(uid, component);
    }

    private void OnShutdown(EntityUid uid, IntentComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.RemoveAction(uid, component.HelpActionEntity, comp);
        _action.RemoveAction(uid, component.DisarmActionEntity, comp);
        _action.RemoveAction(uid, component.GrabActionEntity, comp);
        _action.RemoveAction(uid, component.HarmActionEntity, comp);
    }

    private void OnToggleIntent(EntityUid uid, IntentComponent component, ToggleIntentEvent args)
    {
        if (component.Intent == args.Type)
            return;

        args.Handled = true;

        component.Intent = args.Type;
        Dirty(uid, component);

        UpdateActions(uid, component);
    }

    private void UpdateActions(EntityUid uid, IntentComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.HelpActionEntity, component.Intent == Intent.Help);
        _action.SetToggled(component.DisarmActionEntity, component.Intent == Intent.Disarm);
        _action.SetToggled(component.GrabActionEntity, component.Intent == Intent.Grab);
        _action.SetToggled(component.HarmActionEntity, component.Intent == Intent.Harm);
    }

    public bool CanAttack(EntityUid uid, IntentComponent? component)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.Intent is not Intent.Help;
    }
}

public sealed partial class ToggleIntentEvent : InstantActionEvent
{
    [DataField]
    public Intent Type;
}
