using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Interaction;

namespace Content.Shared._White.Misc.ChristmasLights;

public abstract class SharedChristmasLightsSystem : EntitySystem
{
    [Dependency] protected readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] protected readonly SharedInteractionSystem _interaction = default!;

    protected bool CanInteract(EntityUid uid, EntityUid user) => _actionBlocker.CanInteract(user, uid) && _interaction.InRangeUnobstructed(user, uid);
}
