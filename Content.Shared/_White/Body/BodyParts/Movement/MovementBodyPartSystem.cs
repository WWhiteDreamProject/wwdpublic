using Content.Shared._White.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;

namespace Content.Shared._White.Body.BodyParts.Movement;

public sealed class MovementBodyPartSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MovementBodyPartComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<MovementBodyPartComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
    }

    #region Event Handling

    private void OnGotInserted(Entity<MovementBodyPartComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        //TODO
    }

    private void OnGotRemoved(Entity<MovementBodyPartComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        //TODO
        if (_buckle.IsBuckled(args.Body))
            return;

        _standingState.Down(args.Body);
    }

    #endregion
}
