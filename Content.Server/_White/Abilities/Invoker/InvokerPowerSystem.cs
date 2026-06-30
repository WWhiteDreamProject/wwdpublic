using Content.Shared._White.Abilities.Invoker;

namespace Content.Server._White.Abilities.Invoker;

public sealed class InvokerPowerSystem : SharedInvokerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvokerComponent, InvokerOrbActionEvent>(DoOrb);
        SubscribeLocalEvent<InvokerComponent, InvokerInvokeActionEvent>(DoInvoke);
    }

    private void DoOrb(EntityUid uid, InvokerComponent component, InvokerOrbActionEvent args)
    {
        AddOrb(uid, args.Orb, component);

        args.Handled = true;
    }

    private void DoInvoke(EntityUid uid, InvokerComponent component, InvokerInvokeActionEvent args)
    {
        Invoke(uid, component);

        args.Handled = true;
    }
}
