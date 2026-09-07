using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Layer.Systems;
using Robust.Shared.Utility;

namespace Content.Client._White.Humanoid.Markings.Systems;

public sealed partial class MarkingsSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<MarkingsProviderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyRelayedEvent<HideableLayerVisibilityChangedEvent>>(OnHideableLayerVisibilityChanged);
    }

    #region Event Handling

    private void OnAfterAutoHandleState(Entity<MarkingsProviderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Body is not {} body)
            return;

        if (_spriteQuery.TryComp(body, out var spriteComp))
            return;

        RemoveMarkings((body, spriteComp), ent);
        ApplyMarkings((body, spriteComp), ent);
    }

    private void OnHideableLayerVisibilityChanged(Entity<MarkingsProviderComponent> ent, ref BodyRelayedEvent<HideableLayerVisibilityChangedEvent> args)
    {
        if (ent.Comp.Body is not {} body)
            return;

        if (!ent.Comp.HideableLayers.Contains(args.Args.Layer))
            return;

        foreach (var (layer, markings) in ent.Comp.Markings)
        {
            foreach (var marking in markings)
            {
                if (!Equals(layer, args.Args.Layer) &&
                    !(ent.Comp.DependentHidingLayers.TryGetValue(args.Args.Layer, out var dependent) &&
                        dependent.Contains(layer)))
                    continue;

                if (marking.Sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerId = $"{marking.Id}-{rsi.RsiState}";

                if (!_sprite.LayerMapTryGet(body, layerId, out var index, true))
                    continue;

                _sprite.LayerSetVisible(body, index, args.Args.Visible);
            }
        }
    }

    protected override void OnGotInserted(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        base.OnGotInserted(ent, ref args);

        ApplyMarkings(args.Body.Owner, ent);
    }

    protected override void OnGotRemoved(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        base.OnGotRemoved(ent, ref args);

        RemoveMarkings(args.Body.Owner, ent);
    }

    #endregion

    #region Public API

    public override void SetMarkings(Entity<MarkingsProviderComponent?> ent, Dictionary<Enum, List<Marking>> markings)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        base.SetMarkings(ent, markings);

        if (ent.Comp.Body is not {} body)
            return;

        if (_spriteQuery.TryComp(body, out var spriteComp))
            return;

        RemoveMarkings((body, spriteComp), (ent, ent.Comp));
        ApplyMarkings((body, spriteComp), (ent, ent.Comp));
    }

    #endregion

    #region Private API

    private IEnumerable<Marking> GetMarkings(Entity<MarkingsProviderComponent?> ent)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            yield break;

        foreach (var markings in ent.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                yield return marking;
            }
        }

        if (!CensorNudity)
            yield break;

        if (!Prototype.TryIndex(ent.Comp.Data.Group, out var group))
            yield break;

        var categories = Marking.GetCategories(ent.Comp.Data.Layers);
        foreach (var category in categories)
        {
            if (!group.CategoriesData.TryGetValue(category, out var categoryData))
                continue;

            if (categoryData.Nudity.Count < 1)
                continue;

            var contains = false;
            foreach (var layer in Marking.GetLayers(category))
            {
                if (ent.Comp.Markings.ContainsKey(layer))
                    continue;

                contains = true;
            }

            if (!contains)
                continue;

            foreach (var marking in categoryData.Nudity)
            {
                if (!Marking.TryGetMarking(marking, out var prototype))
                    continue;

                foreach (var definition in prototype.Definitions)
                {
                    yield return new(definition.OverrideAppearance, definition.Layer, prototype.Category, marking, definition.Sprite, definition.Index);
                }
            }
        }
    }

    #endregion
}
