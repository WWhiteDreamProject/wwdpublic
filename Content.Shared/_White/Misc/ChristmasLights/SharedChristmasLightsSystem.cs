using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Interaction;

namespace Content.Shared._White.Misc.ChristmasLights;

public abstract class SharedChristmasLightsSystem : EntitySystem
{
    [Dependency] protected readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] protected readonly SharedInteractionSystem _interaction = default!;

    [Dependency] protected readonly ILocalizationManager _loc = default!;

    public override void Initialize()
    {
        //SubscribeLocalEvent<ChristmasLightsComponent, ExaminedEvent>(OnChristmasLightsExamine);

    }

    protected bool CanInteract(EntityUid uid, EntityUid user) => _actionBlocker.CanInteract(user, uid) && _interaction.InRangeUnobstructed(user, uid);

    private void OnChristmasLightsExamine(EntityUid uid, ChristmasLightsComponent comp, ExaminedEvent args) // todo why am i forced to keep this in shared?
    {
        //args.PushMarkup(_loc.GetString("christmas-lights-examine-toggle-mode-tip"), 1);
        //args.PushMarkup(_loc.GetString("christmas-lights-examine-toggle-brightness-tip"), 0);
    }
}
