using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Clothing.Components;
using Robust.Shared.Log;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class HeadphonesSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadphonesComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(EntityUid uid, HeadphonesComponent component, ItemToggledEvent args)
    {
        var prefix = args.Activated ? "on" : "off";
        _item.SetHeldPrefix(uid, prefix);
        _clothing.SetEquippedPrefix(uid, prefix);
    }
}
