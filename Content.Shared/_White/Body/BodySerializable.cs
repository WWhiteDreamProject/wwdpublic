using Content.Shared._White.Body.Components;

namespace Content.Shared._White.Body;

#region BodyPart



/// <summary>
/// Raised when a body part is attached to body.
/// </summary>
/// <param name="Part">The attached body part.</param>
/// <param name="Body">The body to which the body part was attached.</param>
/// <param name="SlotId">Container ID of Part.</param>
public readonly record struct BodyPartAddedEvent(
    Entity<BodyPartComponent> Part,
    Entity<BodyComponent>? Body,
    string SlotId);

/// <summary>
/// Raised when a body part is detached from body.
/// </summary>
/// <param name="Part">The detached body part.</param>
/// <param name="Body">The body from which the body part was detached.</param>
/// <param name="SlotId">Container ID of Part.</param>
public readonly record struct BodyPartRemovedEvent(
    Entity<BodyPartComponent> Part,
    Entity<BodyComponent>? Body,
    string SlotId);

#endregion

#region Bone

public readonly record struct BoneAddedEvent;

public readonly record struct BoneRemovedEvent;

#endregion

#region Organ

/// <summary>
/// Raised when an organ attaches to body.
/// </summary>
/// <param name="Body">The body to which the organ is attached.</param>
/// <param name="Parent">The parent to which the organ is attached.</param>
public readonly record struct OrganAddedEvent(Entity<BodyComponent>? Body, EntityUid Parent);

/// <summary>
/// Raised when an organ attaches to body.
/// </summary>
/// <param name="Organ">The attached organ.</param>
public readonly record struct OrganAddedToBodyEvent(Entity<OrganComponent> Organ);

/// <summary>
/// Raised when an organ detaches from body.
/// </summary>
/// <param name="Body">The body from which the organ is detached.</param>
/// <param name="Parent">The parent from which the organ is detached.</param>
public readonly record struct OrganRemovedEvent(Entity<BodyComponent>? Body, EntityUid Parent);

/// <summary>
/// Raised when an organ detaches from body.
/// </summary>
/// <param name="Organ">The detached organ.</param>
public readonly record struct OrganRemovedFromBodyEvent(Entity<OrganComponent> Organ);

#endregion
