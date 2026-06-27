namespace Content.Shared._White.Abilities.Invoker;

public abstract class SharedInvokerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvokerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<InvokerComponent> ent, ref ComponentStartup args)
    {
        UpdateOrbVisuals(ent, ent.Comp);
    }

    public void AddOrb(EntityUid uid, OrbType newOrb, InvokerComponent component)
    {
        if (component.CurrentOrbs.Count >= 3)
        {
            component.CurrentOrbs.RemoveAt(0);
        }

        component.CurrentOrbs.Add(newOrb);

        UpdateOrbVisuals(uid, component);
    }

    private void UpdateOrbVisuals(EntityUid uid, InvokerComponent component)
    {
        Dirty(uid, component);

        // Все мои друзья нюхают и колются
        // Попрошу я батюшку пусть помолится
        // За моих друзей, за моих друзей
    }
}
