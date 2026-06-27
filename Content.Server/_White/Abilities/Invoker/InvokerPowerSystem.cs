using Content.Shared._White.Abilities.Invoker;

namespace Content.Server._White.Abilities.Invoker;

public sealed class InvokerPowerSystem : SharedInvokerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvokerComponent, InvokerOrbActionEvent>(DoOrb);
    }

    private void DoOrb(EntityUid uid, InvokerComponent component, InvokerOrbActionEvent args)
    {
        AddOrb(uid, args.Orb, component);
    }
}
