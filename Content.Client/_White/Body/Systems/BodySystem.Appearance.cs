using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeAppearance() =>
        SubscribeLocalEvent<BodyAppearanceComponent, AfterAutoHandleStateEvent>(OnBodyAppearanceHandleState);

    #region Event Handling

    private void OnBodyAppearanceHandleState(Entity<BodyAppearanceComponent> bodyAppearance, ref AfterAutoHandleStateEvent args) =>
        BodySpriteUpdate(bodyAppearance.AsNullable());

    protected override void OnBodyPartRemoved(Entity<BodyAppearanceComponent> bodyAppearance, ref BodyPartRemovedEvent args)
    {
        base.OnBodyPartRemoved(bodyAppearance, ref args);

        if (!TryComp<BodyPartAppearanceComponent>(args.Part, out var bodyPartAppearanceComponent))
            return;

        RemoveMarkingSprites(bodyAppearance.Owner, bodyPartAppearanceComponent.MarkingsLayers);
    }

    protected override void OnOrganRemoved(Entity<BodyAppearanceComponent> bodyAppearance, ref OrganRemovedEvent args)
    {
        base.OnOrganRemoved(bodyAppearance, ref args);

        if (!TryComp<OrganAppearanceComponent>(args.Organ, out var organAppearanceComponent))
            return;

        RemoveMarkingSprites(bodyAppearance.Owner, organAppearanceComponent.MarkingsLayers);
    }

    #endregion

    #region Private API

    #region Marking

    public void AddMarkingSprite(Entity<SpriteComponent?> sprite, MarkingLayerInfo markingLayerInfo, int index)
    {
        if (!Resolve(sprite, ref sprite.Comp)
            || !_sprite.LayerMapTryGet(sprite, markingLayerInfo.Layer, out var layerIndex, false))
            return;

        var layerId = $"{markingLayerInfo.MarkingId}-{markingLayerInfo.State}";
        if (!_sprite.LayerMapTryGet(sprite, layerId, out _, false))
        {
            var spriteSpecifier = new SpriteSpecifier.Rsi(markingLayerInfo.Sprite, markingLayerInfo.State);
            var markingInex = _sprite.AddLayer(sprite, spriteSpecifier, layerIndex + index + 1);

            _sprite.LayerMapSet(sprite, layerId, markingInex);
            _sprite.LayerSetSprite(sprite, layerId, spriteSpecifier);
        }

        _sprite.LayerSetVisible(sprite, layerId, markingLayerInfo.Visible);

        if (!markingLayerInfo.Visible)
            return;

        _sprite.LayerSetColor(sprite, layerId, markingLayerInfo.Color);

        var shaders = markingLayerInfo.Shaders;
        if (shaders is not null && shaders.TryGetValue(markingLayerInfo.State, out var shader))
            sprite.Comp.LayerSetShader(layerId, shader);
    }

    private void AddMarkingSprites(Entity<SpriteComponent?> sprite, List<MarkingLayerInfo> markingsLayers)
    {
        if (!Resolve(sprite, ref sprite.Comp))
            return;

        for (var i = 0; i < markingsLayers.Count; i++)
        {
            var markingLayer = markingsLayers[i];
            AddMarkingSprite(sprite, markingLayer, i);
        }
    }

    private void RemoveMarkingSprite(Entity<SpriteComponent?> sprite, MarkingLayerInfo markingLayer)
    {
        if (!Resolve(sprite, ref sprite.Comp))
            return;

        var layerId = $"{markingLayer.MarkingId}-{markingLayer.State}";
        if (!_sprite.LayerMapTryGet(sprite, layerId, out var index, false))
            return;

        _sprite.LayerMapRemove(sprite, layerId);
        _sprite.RemoveLayer(sprite, index);
    }

    private void RemoveMarkingSprites(Entity<SpriteComponent?> sprite, List<MarkingLayerInfo> markingsLayers)
    {
        if (!Resolve(sprite, ref sprite.Comp))
            return;

        foreach (var markingLayer in markingsLayers)
            RemoveMarkingSprite(sprite, markingLayer);
    }

    private void UpdateMarkingSprite(Entity<SpriteComponent?> sprite, MarkingLayerInfo markingLayer)
    {
        if (!Resolve(sprite, ref sprite.Comp))
            return;

        var layerId = $"{markingLayer.MarkingId}-{markingLayer.State}";
        if (!_sprite.LayerMapTryGet(sprite, layerId, out _, false))
            return;

        _sprite.LayerSetColor(sprite, layerId, markingLayer.Color);

        var shaders = markingLayer.Shaders;
        if (shaders is not null && shaders.TryGetValue(markingLayer.State, out var shader))
            sprite.Comp.LayerSetShader(layerId, shader);
    }

    private void UpdateMarkingSprites(Entity<SpriteComponent?> sprite, List<MarkingLayerInfo> markingsLayers)
    {
        if (!Resolve(sprite, ref sprite.Comp))
            return;

        foreach (var markingLayer in markingsLayers)
            UpdateMarkingSprite(sprite, markingLayer);
    }

    #endregion

    private string GetState(RSI rsi, Sex sex, string? bodyType, string state)
    {
        if (rsi.TryGetState($"{state}_{sex.ToString().ToLower()}", out _))
            state = $"{state}_{sex.ToString().ToLower()}";

        if (bodyType != null && rsi.TryGetState($"{state}_{bodyType.ToLower()}", out _))
            state = $"{state}_{bodyType.ToLower()}";

        return state;
    }

    private void BodySpriteUpdate(Entity<BodyAppearanceComponent?> bodyAppearance)
    {
        if (!Resolve(bodyAppearance, ref bodyAppearance.Comp)
            || !TryComp<SpriteComponent>(bodyAppearance, out var spriteComponent))
            return;

        var sprite = (bodyAppearance.Owner, spriteComponent);
        foreach (var (layer, info) in bodyAppearance.Comp.Layers)
        {
            if (info == null)
            {
                _sprite.LayerSetVisible(sprite, layer, false);
                continue;
            }

            /*RemoveMarkingSprites(sprite, bodyPartAppearance.MarkingsLayers);
            AddMarkingSprites(sprite, bodyPartAppearance.MarkingsLayers);*/

            _sprite.LayerSetVisible(sprite, layer, info.Visible);

            if (!info.Visible)
                continue;

            _sprite.LayerSetColor(sprite, layer, info.Color);

            var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / info.Sprite).RSI;
            _sprite.LayerSetRsi(sprite, layer, rsi, GetState(rsi, info.Sex, info.BodyType, info.State));
        }
    }

    #endregion

    #region Public API

    public override void SetupBodyAppearance(Entity<BodyComponent?, BodyAppearanceComponent?, HumanoidAppearanceComponent?> body, bool sync = true)
    {
        base.SetupBodyAppearance(body, false);
        BodySpriteUpdate((body, body.Comp2));
    }

    public override void SetBodyPartColor(Entity<BodyPartAppearanceComponent?> bodyPartAppearance, Color color, bool sync = true)
    {
        if (!Resolve(bodyPartAppearance, ref bodyPartAppearance.Comp) || !bodyPartAppearance.Comp.CanChangeColor)
            return;

        bodyPartAppearance.Comp.Color = color;

        if (!TryComp<BodyPartComponent>(bodyPartAppearance, out var bodyPart) || !bodyPart.Body.HasValue)
            return;

        _sprite.LayerSetColor(bodyPart.Body.Value, bodyPartAppearance.Comp.Layer, color);
    }

    public override void SetBodyColor(Entity<BodyComponent?, BodyAppearanceComponent?> body, Color color, bool sync = true)
    {
        if (!Resolve(body, ref body.Comp2))
            return;

        base.SetBodyColor(body, color, false);

        if (!TryComp<SpriteComponent>(body, out var spriteComponent))
            return;

        var sprite = (body.Owner, spriteComponent);
        foreach (var (layer, info) in body.Comp2.Layers)
        {
            if (info == null)
                continue;

            _sprite.LayerSetColor(sprite, layer, info.Color);
        }
    }

    #region Marking

    public override void AddMarking(Entity<BodyComponent?, HumanoidAppearanceComponent?> body, Marking marking)
    {
        if (!Resolve(body, ref body.Comp1, false)
            || !Resolve(body, ref body.Comp2, false)
            || !_marking.TryGetMarking(marking, out var markingPrototype)
            || !TryComp<SpriteComponent>(body, out var spriteComponent))
            return;

        var sprite = (body.Owner, spriteComponent);
        for (var i = 0; i < markingPrototype.Sprites.Count; i++)
        {
            var markingSprite = new MarkingLayerInfo(markingPrototype.Sprites[i]);

            markingSprite.Color = marking.MarkingColors[i];
            markingSprite.Shaders = markingPrototype.Shaders;

            if (markingSprite.Organ != OrganType.None && GetOrgans<OrganAppearanceComponent>((body, body.Comp1), markingSprite.Organ).FirstOrNull() is { } organ)
            {
                organ.Comp2.MarkingsLayers.Add(markingSprite);
                AddMarkingSprite(sprite, markingSprite, i);

                continue;
            }

            if (GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1), markingSprite.BodyPart).FirstOrNull() is not { } bodyPart)
            {
                if (!markingSprite.ReplacementBodyPart.HasValue)
                    continue;

                var bodyPartUid = Spawn(markingSprite.ReplacementBodyPart);

                if (!TryComp<BodyPartComponent>(bodyPartUid, out var bodyPartComponent)
                    || !TryComp<BodyPartAppearanceComponent>(bodyPartUid, out var bodyPartAppearanceComponent)
                    || !TryAttachBodyPart((body, body.Comp1), (bodyPartUid, bodyPartComponent)))
                {
                    QueueDel(bodyPartUid);
                    continue;
                }

                bodyPartAppearanceComponent.IsMarking = true;
                bodyPart = (bodyPartUid, bodyPartComponent, bodyPartAppearanceComponent);
            }

            if (markingSprite.ReplacementBodyPart.HasValue && bodyPart.Comp2.Visible)
            {
                bodyPart.Comp2.Visible = false;
                _sprite.LayerSetVisible(sprite, bodyPart.Comp2.Layer, false);
            }

            bodyPart.Comp2.MarkingsLayers.Add(markingSprite);

            AddMarkingSprite(sprite, markingSprite, i);
        }
    }

    public override void RemoveMarking(Entity<BodyComponent?, HumanoidAppearanceComponent?> body, Marking marking)
    {
        if (!Resolve(body, ref body.Comp1, false)
            || !Resolve(body, ref body.Comp2, false)
            || !_marking.TryGetMarking(marking, out var markingPrototype)
            || !TryComp<SpriteComponent>(body, out var spriteComponent))
            return;

        var sprite = (body.Owner, spriteComponent);
        foreach (var markingSprite in markingPrototype.Sprites)
        {
            if (markingSprite.Organ != OrganType.None && GetOrgans<OrganAppearanceComponent>((body, body.Comp1), markingSprite.Organ).FirstOrNull() is { } organ)
            {
                organ.Comp2.MarkingsLayers.Remove(markingSprite);
                continue;
            }

            if (markingSprite.BodyPart == BodyPartType.None || GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1), markingSprite.BodyPart).FirstOrNull() is not { } bodyPart)
                continue;

            bodyPart.Comp2.MarkingsLayers.Remove(markingSprite);

            if (markingSprite.ReplacementBodyPart.HasValue && !bodyPart.Comp2.Visible)
            {
                bodyPart.Comp2.Visible = true;
                _sprite.LayerSetVisible(sprite, bodyPart.Comp2.Layer, true);
            }

            if (markingSprite.ReplacementBodyPart.HasValue && bodyPart.Comp2.IsMarking)
            {
                QueueDel(bodyPart);
                continue;
            }

            RemoveMarkingSprite(sprite, markingSprite);
        }
    }

    public override void UpdateMarking(Entity<BodyComponent?, HumanoidAppearanceComponent?> body, Marking marking)
    {
        if (!Resolve(body, ref body.Comp1, false)
            || !Resolve(body, ref body.Comp2, false)
            || !_marking.TryGetMarking(marking, out var markingPrototype)
            || !TryComp<SpriteComponent>(body, out var spriteComponent))
            return;

        var sprite = (body.Owner, spriteComponent);
        for (var i = 0; i < markingPrototype.Sprites.Count; i++)
        {
            var prototypeMarkingSprite = new MarkingLayerInfo(markingPrototype.Sprites[i]);

            List<MarkingLayerInfo> markingLayers;
            if (GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1), prototypeMarkingSprite.BodyPart).FirstOrNull() is { } bodyPart)
                markingLayers = bodyPart.Comp2.MarkingsLayers;
            else if (GetOrgans<OrganAppearanceComponent>((body, body.Comp1), prototypeMarkingSprite.Organ).FirstOrNull() is { } organ)
                markingLayers = organ.Comp2.MarkingsLayers;
            else
                continue;

            if (!TryGetMarkingLayer(markingLayers, prototypeMarkingSprite.MarkingId, prototypeMarkingSprite.State, out var markingSprite))
                continue;

            markingSprite.Color = marking.MarkingColors[i];
            markingSprite.Shaders = markingPrototype.Shaders;

            UpdateMarkingSprite(sprite, markingSprite);
        }
    }

    #endregion

    #endregion
}
