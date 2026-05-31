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

        foreach (var marking in ent.Comp.Markings)
        {
            if (!Equals(marking.Layer, args.Args.Layer) && !(ent.Comp.DependentHidingLayers.TryGetValue(args.Args.Layer, out var dependent) && dependent.Contains(marking.Layer)))
                continue;

            if (marking.Sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{marking.Id}-{rsi.RsiState}";

            if (!_sprite.LayerMapTryGet(body, layerId, out var index, true))
                continue;

            _sprite.LayerSetVisible(body, index, args.Args.Visible);
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

    public override void SetMarkings(Entity<MarkingsProviderComponent?> ent, List<Marking> markings)
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
}
