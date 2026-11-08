using Content.Shared._White.Appearance.Components;
using Content.Shared.Humanoid;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._White.Appearance.Systems;

public sealed partial class BodyAppearanceSystem
{
    #region Private API

    private static string GetState(RSI rsi, string state, string bodyType, Sex sex)
    {
        var bodyTypeState = $"{state}_{bodyType.ToLower()}";
        if (rsi.TryGetState(bodyTypeState, out _))
            state = bodyTypeState;

        var sexState = $"{state}_{sex.ToString().ToLower()}";
        if (rsi.TryGetState(sexState, out _))
            state = sexState;

        return state;
    }

    protected override void ApplyAppearance(Entity<BodyAppearanceProviderComponent> ent)
    {
        if (!_spriteQuery.TryComp(ent, out var spriteComp))
            return;

        for (var i = 0; i < ent.Comp.Entries.Count; i++)
        {
            var entry = ent.Comp.Entries[i];

            var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / entry.Path).RSI;
            var state = GetState(rsi, entry.State, ent.Comp.Appearance.BodyType, ent.Comp.Appearance.Sex);

            if (!_sprite.LayerMapTryGet((ent, spriteComp), entry.Index, out var entryIndex, false))
            {
                var specifier = new SpriteSpecifier.Rsi(rsi.Path, state);
                entryIndex = _sprite.AddLayer((ent, spriteComp), specifier, i + 1);
                _sprite.LayerMapSet((ent, spriteComp), entry.Index, entryIndex);
            }

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

        if (ent.Comp.Entries.Count == 1)
        {
            var entry = ent.Comp.Entries[0];

            var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / entry.Path).RSI;
            var state = GetState(rsi, entry.State, ent.Comp.Appearance.BodyType, ent.Comp.Appearance.Sex);

            _sprite.LayerSetRsi((target, spriteComp), layerIndex, rsi, state);
            _sprite.LayerSetColor((target, spriteComp), layerIndex, entry.Color);
            return;
        }

        for (var i = 0; i < ent.Comp.Entries.Count; i++)
        {
            var entry = ent.Comp.Entries[i];

            var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / entry.Path).RSI;
            var state = GetState(rsi, entry.State, ent.Comp.Appearance.BodyType, ent.Comp.Appearance.Sex);

            var key = $"{ent.Comp.Layer.ToString()}-{entry.Index}";
            if (!_sprite.LayerMapTryGet((target, spriteComp), key, out var entryIndex, false))
            {
                var specifier = new SpriteSpecifier.Rsi(rsi.Path, state);
                entryIndex = _sprite.AddLayer((target, spriteComp), specifier, layerIndex + i + 1);
                _sprite.LayerMapSet((target, spriteComp), key, entryIndex);
            }

            _sprite.LayerSetRsi((target, spriteComp), entryIndex, rsi, state);
            _sprite.LayerSetColor((target, spriteComp), entryIndex, entry.Color);
        }
    }

    protected override void RemoveAppearance(Entity<BodyAppearanceProviderComponent> ent)
    {
        if (!_spriteQuery.TryComp(ent, out var spriteComp))
            return;

        foreach (var entry in ent.Comp.Entries)
        {
            if (!_sprite.LayerMapTryGet((ent, spriteComp), entry.Index, out var entryIndex, false))
                continue;

            _sprite.RemoveLayer((ent, spriteComp), entryIndex);
        }
    }

    protected override void RemoveAppearance(Entity<BodyAppearanceProviderComponent> ent, EntityUid target)
    {
        if (!_spriteQuery.TryComp(target, out var spriteComp))
            return;

        if (ent.Comp.Entries.Count == 1)
        {
            if (!_sprite.LayerMapTryGet((target, spriteComp), ent.Comp.Layer, out var layerIndex, true))
                return;

            _sprite.LayerSetRsiState(target, layerIndex, RSI.StateId.Invalid);
            return;
        }

        foreach (var entry in ent.Comp.Entries)
        {
            var key = $"{ent.Comp.Layer.ToString()}-{entry.Index}";
            if (!_sprite.LayerMapTryGet((target, spriteComp), key, out var layerIndex, true))
                return;

            _sprite.RemoveLayer((target, spriteComp), layerIndex);
        }
    }

    #endregion
}
