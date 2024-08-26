using Content.Shared.Actions;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;

namespace Content.Shared._White.Intent;

public abstract class SharedIntentSystem : EntitySystem
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

        SetMouseRotatorComponents(uid, false);
    }

    private void OnToggleIntent(EntityUid uid, IntentComponent component, ToggleIntentEvent args)
    {
        if (component.Intent == args.Type)
            return;

        args.Handled = true;

        component.Intent = args.Type;
        Dirty(uid, component);

        UpdateActions(uid, component);

        if (!component.ToggleMouseRotator || IsNpc(uid))
            return;

        if (args.Type == Intent.Harm)
        {
            SetMouseRotatorComponents(uid, true);
            return;
        }

        SetMouseRotatorComponents(uid, false);
    }

    private void UpdateActions(EntityUid uid, IntentComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.HelpActionEntity, component.Intent == Intent.Help);
        _action.SetToggled(component.DisarmActionEntity, component.Intent == Intent.Disarm);
        _action.SetToggled(component.GrabActionEntity, component.Intent == Intent.Grab);
        _action.SetToggled(component.HarmActionEntity, component.Intent == Intent.Harm);
    }

    private void SetMouseRotatorComponents(EntityUid uid, bool value)
    {
        if (value)
        {
            EnsureComp<MouseRotatorComponent>(uid);
            EnsureComp<NoRotateOnMoveComponent>(uid);
        }
        else
        {
            RemComp<MouseRotatorComponent>(uid);
            RemComp<NoRotateOnMoveComponent>(uid);
        }
    }

    public bool CanAttack(EntityUid? uid, IntentComponent? component = null)
    {
        if (uid == null || !Resolve(uid.Value, ref component))
            return false;

        return component.Intent is not Intent.Help;
    }

    public virtual void SetIntent(EntityUid uid, Intent intent = Intent.Help, IntentComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Intent = intent;

        UpdateActions(uid, component);
    }

    public void SetCanDisarm(EntityUid uid, bool canDisarm, IntentComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.CanDisarm = canDisarm;
    }

    public Intent? GetIntent(EntityUid? uid, IntentComponent? component = null)
    {
        if (uid == null || !Resolve(uid.Value, ref component))
            return null;

        return component.Intent;
    }

    protected abstract bool IsNpc(EntityUid uid);
}

public sealed partial class ToggleIntentEvent : InstantActionEvent
{
    [DataField]
    public Intent Type = Intent.Harm;
}

public sealed class DisarmedEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The entity being disarmed.
    /// </summary>
    public EntityUid Target { get; init; }

    /// <summary>
    ///     The entity performing to disarm.
    /// </summary>
    public EntityUid Source { get; init; }

    /// <summary>
    ///     Probability for push/knockdown.
    /// </summary>
    public float PushProbability { get; init; }
}
