using Content.Shared._White.Abilities.Invoker;
using Content.Shared.Actions;
using Content.Shared.Interaction.Events;

namespace Content.Server._White.Invoker;

public sealed class InvokerBookSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InvokerBookComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(EntityUid uid, InvokerBookComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;

        EnsureComp<InvokerComponent>(user);

        _actions.AddAction(user, "ActionInvokerQuas");
        _actions.AddAction(user, "ActionInvokerWex");
        _actions.AddAction(user, "ActionInvokerExort");
        _actions.AddAction(user, "ActionInvokerInvoke");

        args.Handled = true;
        QueueDel(uid);
    }
}
