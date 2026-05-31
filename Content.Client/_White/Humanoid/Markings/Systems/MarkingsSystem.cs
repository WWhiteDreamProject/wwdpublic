using Content.Client.DisplacementMap;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Markings.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._White.Humanoid.Markings.Systems;

public sealed partial class MarkingsSystem : SharedMarkingsSystem
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeProvider();

        _spriteQuery = GetEntityQuery<SpriteComponent>();
    }

    #region Private API

    private void ApplyMarkings(Entity<SpriteComponent?> ent, Entity<MarkingsProviderComponent> provider)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp))
            return;

        var applied = new List<Marking>();
        var counter = new Dictionary<ProtoId<MarkingPrototype>, int>();

        foreach (var marking in GetMarkings(provider.AsNullable()))
        {
            if (!Marking.TryGetMarking(marking, out var prototype))
                continue;

            if (!_sprite.LayerMapTryGet(ent, marking.Layer, out var index, true))
                continue;

            provider.Comp.Displacement.TryGetValue(marking.Layer, out var displacement);

            if (marking.Sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{marking.Id}-{rsi.RsiState}";

            var markingIndex = counter.GetValueOrDefault(marking.Id);
            counter[marking.Id] = markingIndex + 1;

            if (!_sprite.LayerMapTryGet(ent, layerId, out _, false))
            {
                var spriteLayer = _sprite.AddLayer(ent, marking.Sprite, index + markingIndex + 1);
                _sprite.LayerMapSet(ent, layerId, spriteLayer);
                _sprite.LayerSetSprite(ent, layerId, rsi);
            }

            _sprite.LayerSetColor(ent, layerId, marking.Color);

            if (displacement != null && prototype.CanBeDisplaced)
                _displacement.TryAddDisplacement(displacement, ent.Comp, index + markingIndex + 1, layerId, new());

            applied.Add(marking);
        }

        provider.Comp.Applied = applied;
    }

    private void RemoveMarkings(Entity<SpriteComponent?> ent, Entity<MarkingsProviderComponent> provider)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp))
            return;

        foreach (var marking in provider.Comp.Applied)
        {
            if (marking.Sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{marking.Id}-{rsi.RsiState}";

            if (!_sprite.LayerMapTryGet(ent, layerId, out var index, false))
                continue;

            _sprite.LayerMapRemove(ent, layerId);
            _sprite.RemoveLayer(ent, index);
        }
    }

    #endregion
}
