using Robust.Shared.Map;

namespace Content.Shared._White.Move;

[ByRefEvent]
public readonly struct MoveEventProxy(
    Entity<TransformComponent, MetaDataComponent> entity,
    EntityCoordinates oldPos,
    EntityCoordinates newPos,
    Angle oldRotation,
    Angle newRotation)
{
    public readonly Entity<TransformComponent, MetaDataComponent> Entity = entity;
    public readonly EntityCoordinates OldPosition = oldPos;
    public readonly EntityCoordinates NewPosition = newPos;
    public readonly Angle OldRotation = oldRotation;
    public readonly Angle NewRotation = newRotation;

    public EntityUid Sender => Entity.Owner;
    public TransformComponent Component => Entity.Comp1;

    public bool ParentChanged => NewPosition.EntityId != OldPosition.EntityId;
}
