using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._White.Guns.AmmoProviderRedirect;

public sealed class AmmoProviderRedirectSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParentAmmoProviderComponent, TakeAmmoEvent>(RedirectParent);
    }

    // todo figure out a way to check bullets' caliber before firing
    // don't let people feed .50 cal rounds into methaphorical pea shooters
    private void RedirectParent(EntityUid uid, ParentAmmoProviderComponent comp, TakeAmmoEvent args)
    {
        if (_transform.GetParentUid(uid) is { Valid: true } parent)
            RaiseLocalEvent(parent, args);
    }
}
