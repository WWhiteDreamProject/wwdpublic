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
        if (!BodyQuery.HasComp(args.Body))
            return;

        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Body));

        ApplyAppearance(ent, args.Body.Owner);
    }

    protected override void OnGotRemoved(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (!BodyQuery.HasComp(args.Body))
            return;

        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Body));

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

    public override void SetPath(Entity<BodyAppearanceProviderComponent?> ent, string path)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        base.SetPath(ent, path);

        if (ent.Comp.Body is not {} body)
            return;

        ApplyAppearance((ent, ent.Comp), body);
    }

    public override void SetState(Entity<BodyAppearanceProviderComponent?> ent, string state)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        base.SetState(ent, state);

        if (ent.Comp.Body is not {} body)
            return;

        ApplyAppearance((ent, ent.Comp), body);
    }

    #endregion

    #region Private API

    private void ApplyAppearance(Entity<BodyAppearanceProviderComponent> ent, Entity<SpriteComponent?> target)
    {
        var rsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / ent.Comp.Path).RSI;
        var state = ent.Comp.State;

        var bodyTypeState = $"{state}_{ent.Comp.Appearance.BodyType.Id.ToLower()}";
        if (rsi.TryGetState(bodyTypeState, out _))
            state = bodyTypeState;

        var sexState = $"{state}_{ent.Comp.Appearance.Sex.ToString().ToLower()}";
        if (rsi.TryGetState(sexState, out _))
            state = sexState;

        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsi(target, index, rsi);
        _sprite.LayerSetRsiState(target, index, state);
        _sprite.LayerSetColor(target, index, ent.Comp.Color);
    }

    private void RemoveAppearance(Entity<BodyAppearanceProviderComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);
    }

    #endregion
}
