using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Layer.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._White.Humanoid.Markings.Systems;

public sealed partial class MarkingsSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<MarkingsProviderComponent, BodyRelayedEvent<HideableLayerVisibilityChangedEvent>>(OnHideableLayerVisibilityChanged);
    }

    #region Event Handling

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

    #endregion

    #region Private API

    protected override void ApplyMarkings(Entity<MarkingsProviderComponent> ent, EntityUid target)
    {
        if (!_spriteQuery.TryComp(target, out var spriteComp))
            return;

        var counter = new Dictionary<ProtoId<MarkingPrototype>, int>();

        foreach (var marking in ent.Comp.Applied)
        {
            if (!Marking.TryGetMarking(marking, out var prototype))
                continue;

            if (prototype.Appearance)
                continue;

            if (!_sprite.LayerMapTryGet((target, spriteComp), marking.Layer, out var index, true))
                continue;

            ent.Comp.Displacement.TryGetValue(marking.Layer, out var displacement);

            if (marking.Sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{marking.Id}-{rsi.RsiState}";

            var markingIndex = counter.GetValueOrDefault(marking.Id);
            counter[marking.Id] = markingIndex + 1;

            if (!_sprite.LayerMapTryGet((target, spriteComp), layerId, out var layerIndex, false))
            {
                layerIndex = _sprite.AddLayer((target, spriteComp), marking.Sprite, index + markingIndex + 1);
                _sprite.LayerMapSet((target, spriteComp), layerId, layerIndex);
                _sprite.LayerSetSprite((target, spriteComp), layerIndex, rsi);
            }

            _sprite.LayerSetColor((target, spriteComp), layerIndex, marking.Color);

            if (!string.IsNullOrEmpty(marking.Shader))
                spriteComp.LayerSetShader(layerIndex, marking.Shader); // Why its stil not in SpriteSystem?

            if (!prototype.CanBeDisplaced || displacement == null)
                continue;

            _displacement.TryAddDisplacement(displacement, spriteComp, index + markingIndex + 1, layerId, new());
        }
    }

    protected override void RemoveMarkings(Entity<MarkingsProviderComponent> ent, EntityUid target)
    {
        if (!_spriteQuery.TryComp(target, out var spriteComp))
            return;

        foreach (var marking in ent.Comp.Applied)
        {
            if (marking.Sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{marking.Id}-{rsi.RsiState}";

            if (!_sprite.LayerMapTryGet((target, spriteComp), layerId, out var index, false))
                continue;

            _sprite.LayerMapRemove((target, spriteComp), layerId);
            _sprite.RemoveLayer((target, spriteComp), index);
        }
    }

    protected override void UpdateMarkings(Entity<MarkingsProviderComponent> ent)
    {
        foreach (var entity in ent.Comp.Entities)
        {
            RemoveMarkings(ent, entity);
        }

        ent.Comp.Applied.Clear();
        foreach (var markings in ent.Comp.Markings.Values)
        {
            ent.Comp.Applied.AddRange(markings);
        }

        if (CensorNudity && Prototype.TryIndex(ent.Comp.Data.Group, out var group))
        {
            var categories = Marking.GetCategories(ent.Comp.Data.Layers);
            foreach (var category in categories)
            {
                if (!group.Data.TryGetValue(category, out var data))
                    continue;

                if (data.Nudity.Count < 1)
                    continue;

                var contains = false;
                foreach (var layer in Marking.GetLayers(category))
                {
                    if (!ent.Comp.Markings.ContainsKey(layer))
                        continue;

                    contains = true;
                }

                if (contains)
                    continue;

                foreach (var markingId in data.Nudity)
                {
                    if (!Marking.TryGetMarking(markingId, out var prototype))
                        continue;

                    foreach (var definition in prototype.Definitions)
                    {
                        var marking = new Marking(
                            Color.White,
                            definition.Layer,
                            prototype.Category,
                            markingId,
                            definition.Sprite,
                            definition.Index,
                            definition.Shader);
                        ent.Comp.Applied.Add(marking);
                    }
                }
            }
        }

        foreach (var entity in ent.Comp.Entities)
        {
            ApplyMarkings(ent, entity);
        }
    }

    #endregion
}
