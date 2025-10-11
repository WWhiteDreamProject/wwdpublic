using Content.Shared._White.BloodCult.Runes.Components;

namespace Content.Server._White.BloodCult.Runes.Barrier;

public sealed class CultRuneBarrierSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<CultRuneBarrierComponent, InvokeRuneEvent>(OnBarrierRuneInvoked);
        SubscribeLocalEvent<CultRuneBarrierComponent, AfterInvokeRuneEvent>(AfterBarrierRuneInvoked);
    }

    private void OnBarrierRuneInvoked(Entity<CultRuneBarrierComponent> ent, ref InvokeRuneEvent args)
    {
        Spawn(ent.Comp.SpawnPrototype, Transform(ent).Coordinates);
    }

    private void AfterBarrierRuneInvoked(Entity<CultRuneBarrierComponent> ent, ref AfterInvokeRuneEvent args)
    {
        QueueDel(ent);
    }
}
