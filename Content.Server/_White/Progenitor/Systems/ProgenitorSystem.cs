using Content.Server._White.Progenitor.Components;
using Content.Server.Mind;
using Content.Server.Zombies;
using Content.Shared._White.Body.Systems;
using Content.Shared.Zombies;

namespace Content.Server._White.Progenitor.Systems;

public sealed class ProgenitorSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProgenitorProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
    }

    #region Event Handling

    private void OnGotRemoved(Entity<ProgenitorProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Body))
            return;

        var coordinates = Transform(ent).Coordinates;
        var progenitor = Spawn(ent.Comp.Prototype, coordinates);

        if (HasComp<ZombieComponent>(args.Body)) // TODO: ZombieSystem should have IsZombie function.
            _zombie.ZombifyEntity(progenitor);

        if (ent.Comp.TransferMind && _mind.TryGetMind(args.Body, out var mindId, out var mind))
            _mind.TransferTo(mindId, progenitor, mind: mind);

        QueueDel(ent);
    }

    #endregion
}
