using Content.Shared._White.Appearance.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._White.Appearance.Systems;

public sealed partial class BodyAppearanceSystem
{
    #region Private API

    protected override void ApplyAppearance(Entity<BodyAppearanceProviderComponent> ent)
    {
        if (!_spriteQuery.TryComp(ent, out var spriteComp))
            return;

        foreach (var entry in ent.Comp.Entries)
        {
            if (!_sprite.LayerMapTryGet((ent, spriteComp), entry.Index, out var entryIndex, false))
                continue;

            var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / entry.Path).RSI;
            var state = entry.State;

            var bodyTypeState = $"{state}_{ent.Comp.Appearance.BodyType.Id.ToLower()}";
            if (rsi.TryGetState(bodyTypeState, out _))
                state = bodyTypeState;

            var sexState = $"{state}_{ent.Comp.Appearance.Sex.ToString().ToLower()}";
            if (rsi.TryGetState(sexState, out _))
                state = sexState;

            _sprite.LayerSetRsi((ent, spriteComp), entryIndex, rsi, state);
            _sprite.LayerSetColor((ent, spriteComp), entryIndex, entry.Color);
        }
    }

    protected override void ApplyAppearance(Entity<BodyAppearanceProviderComponent> ent, EntityUid target)
    {
        if (!_spriteQuery.TryComp(target, out var spriteComp))
            return;

        if (!_sprite.LayerMapTryGet((target, spriteComp), ent.Comp.Layer, out var layerIndex, true))
            return;

        for (var i = 0; i < ent.Comp.Entries.Count; i++)
        {
            var entry = ent.Comp.Entries[i];

            var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / entry.Path).RSI;
            var state = entry.State;

            var bodyTypeState = $"{state}_{ent.Comp.Appearance.BodyType.Id.ToLower()}";
            if (rsi.TryGetState(bodyTypeState, out _))
                state = bodyTypeState;

            var sexState = $"{state}_{ent.Comp.Appearance.Sex.ToString().ToLower()}";
            if (rsi.TryGetState(sexState, out _))
                state = sexState;

            var key = $"{ent.Comp.Layer.ToString()}-{entry.Index}";
            if (!_sprite.LayerMapTryGet((target, spriteComp), key, out var entryIndex, false))
            {
                var specifier = new SpriteSpecifier.Rsi(rsi.Path, state);
                layerIndex = _sprite.AddLayer((target, spriteComp), specifier, layerIndex + i + 1);
                _sprite.LayerMapSet((target, spriteComp), key, layerIndex);
            }

            _sprite.LayerSetRsi((target, spriteComp), entryIndex, rsi, state);
            _sprite.LayerSetColor((target, spriteComp), entryIndex, entry.Color);
        }
    }

    protected override void RemoveAppearance(Entity<BodyAppearanceProviderComponent> ent, EntityUid target)
    {
        foreach (var entry in ent.Comp.Entries)
        {
            var key = $"{ent.Comp.Layer.ToString()}-{entry.Index}";
            if (!_sprite.LayerMapTryGet(target, key, out var layerIndex, true))
                return;

            _sprite.LayerSetRsiState(target, layerIndex, RSI.StateId.Invalid);
        }
    }

    #endregion
}
