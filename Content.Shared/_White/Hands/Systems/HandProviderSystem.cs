using Content.Shared._White.Body.Systems;
using Content.Shared._White.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._White.Hands.Systems;

public sealed class HandProviderSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<HandProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<HandProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        _hands.AddHand(args.Body, ent.Comp.Id, ent.Comp.Location);
    }

    private void OnGotRemoved(Entity<HandProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Body))
            return;

        _hands.RemoveHand(args.Body, ent.Comp.Id);
    }
}
