using Content.Shared.Throwing;

namespace Content.Server._White.Throwing;

public sealed class ThrowingItemModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowingItemModifierComponent, BeforeGettingThrownEvent>(OnGettingThrown);
    }

    public void OnGettingThrown(EntityUid uid, ThrowingItemModifierComponent component, ref BeforeGettingThrownEvent args) =>
        args.ThrowSpeed *= component.ThrowingMultiplier;
}
