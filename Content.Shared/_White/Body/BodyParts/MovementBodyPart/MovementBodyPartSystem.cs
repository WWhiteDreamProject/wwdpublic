using Content.Shared.Buckle;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;

namespace Content.Shared._White.Body.BodyParts.MovementBodyPart;

public sealed class MovementBodyPartSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MovementBodyPartComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<MovementBodyPartComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
    }

    #region Event Handling

    private void OnBodyPartAdded(Entity<MovementBodyPartComponent> movementBodyPart, ref BodyPartAddedEvent args)
    {
        if (!args.Body.HasValue)
            return;

        //TODO
    }

    private void OnBodyPartRemoved(Entity<MovementBodyPartComponent> movementBodyPart, ref BodyPartRemovedEvent args)
    {
        if (!args.Body.HasValue || _buckle.IsBuckled(args.Body.Value))
            return;

        _standingState.Down(args.Body.Value);
    }

    #endregion
}
