using Content.Shared._White.Appearance;
using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Body.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._White.Appearance.Systems;

public sealed partial class BodyAppearanceSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<BodyAppearanceProviderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    #region Event Handling

    private void OnAfterAutoHandleState(Entity<BodyAppearanceProviderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Body is not {} body)
            return;

        ApplyAppearance(ent, body);
    }

    protected override void OnGotInserted(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        base.OnGotInserted(ent, ref args);

        ApplyAppearance(ent, args.Body.Owner);
    }

    protected override void OnGotRemoved(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        base.OnGotRemoved(ent, ref args);

        RemoveAppearance(ent, args.Body.Owner);
    }

    #endregion

    #region Public API

    public override void SetAppearanceData(Entity<BodyAppearanceProviderComponent?> ent, BodyAppearanceData appearance)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        base.SetAppearanceData(ent, appearance);

        if (ent.Comp.Body is not {} body)
            return;

        ApplyAppearance((ent, ent.Comp), body);
    }

    public override void SetColor(Entity<BodyAppearanceProviderComponent?> ent, Color color)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        base.SetColor(ent, color);

        if (ent.Comp.Body is not {} body)
            return;

        ApplyAppearance((ent, ent.Comp), body);
    }

    public override void SetLayerData(Entity<BodyAppearanceProviderComponent?> ent, PrototypeLayerData data)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        base.SetLayerData(ent, data);

        if (ent.Comp.Body is not {} body)
            return;

        ApplyAppearance((ent, ent.Comp), body);
    }

    #endregion

    #region Private API

    private void ApplyAppearance(Entity<BodyAppearanceProviderComponent> ent, Entity<SpriteComponent?> target)
    {
        if (string.IsNullOrEmpty(ent.Comp.Data.RsiPath))
            return;

        var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / ent.Comp.Data.RsiPath).RSI;

        var bodyTypeState = $"{ent.Comp.Data.State}_{ent.Comp.Appearance.BodyType.Id.ToLower()}";
        if (rsi.TryGetState(bodyTypeState, out _))
            ent.Comp.Data.State = bodyTypeState;

        var sexState = $"{ent.Comp.Data.State}_{ent.Comp.Appearance.Sex.ToString().ToLower()}";
        if (rsi.TryGetState(sexState, out _))
            ent.Comp.Data.State = sexState;

        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetData(target, index, ent.Comp.Data);
    }

    private void RemoveAppearance(Entity<BodyAppearanceProviderComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);
    }

    #endregion
}
