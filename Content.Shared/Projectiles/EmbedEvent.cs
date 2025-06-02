using Content.Shared._Shitmed.Targeting;

namespace Content.Shared.Projectiles;

/// <summary>
/// Raised directed on an entity when it embeds in another entity.
/// </summary>
[ByRefEvent]
public readonly record struct EmbedEvent(EntityUid? Shooter, EntityUid Embedded, TargetBodyPart? BodyPart);

/// <summary>
/// Raised on an entity when it stops embedding in another entity.
/// </summary>
[ByRefEvent]
public readonly record struct RemoveEmbedEvent(EntityUid? Remover);

// WD EDIT START
[ByRefEvent]
public readonly record struct AttemptEmbedEvent(EntityUid? Shooter, EntityUid Embedded);
// WD EDIT END
