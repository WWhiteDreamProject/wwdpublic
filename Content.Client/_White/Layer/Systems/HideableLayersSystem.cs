using Content.Shared._White.Layer.Components;
using Content.Shared._White.Layer.Systems;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;

namespace Content.Client._White.Layer.Systems;

public sealed class HideableLayersSystem : SharedHideableLayersSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HideableLayersComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HideableLayersComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnComponentInit(Entity<HideableLayersComponent> ent, ref ComponentInit args)
    {
        UpdateSprite(ent);
    }

    private void OnHandleState(Entity<HideableLayersComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    public override void SetLayerOcclusion(
        Entity<HideableLayersComponent?> ent,
        Enum layer,
        bool visible,
        SlotFlags source
        )
    {
        base.SetLayerOcclusion(ent, layer, visible, source);

        if (Resolve(ent, ref ent.Comp))
            UpdateSprite((ent, ent.Comp));
    }

    private void UpdateSprite(Entity<HideableLayersComponent> ent)
    {
        foreach (var item in ent.Comp.LastHiddenLayers)
        {
            if (ent.Comp.HiddenLayers.ContainsKey(item))
                continue;

            var ev = new LayerVisibilityChangedEvent(item, true);
            RaiseLocalEvent(ent, ref ev);

            if (!_sprite.LayerMapTryGet(ent.Owner, item, out var index, true))
                continue;

            _sprite.LayerSetVisible(ent.Owner, index, true);
        }

        foreach (var item in ent.Comp.HiddenLayers.Keys)
        {
            if (ent.Comp.LastHiddenLayers.Contains(item))
                continue;

            var ev = new LayerVisibilityChangedEvent(item, false);
            RaiseLocalEvent(ent, ref ev);

            if (!_sprite.LayerMapTryGet(ent.Owner, item, out var index, true))
                continue;

            _sprite.LayerSetVisible(ent.Owner, index, false);
        }

        ent.Comp.LastHiddenLayers.Clear();
        ent.Comp.LastHiddenLayers.UnionWith(ent.Comp.HiddenLayers.Keys);
    }
}
