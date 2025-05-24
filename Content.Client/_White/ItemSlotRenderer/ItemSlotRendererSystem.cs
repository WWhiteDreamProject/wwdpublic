using Content.Client.Hands;
using Content.Shared._White.Hands.Components;
using Content.Shared.Containers.ItemSlots;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.ItemSlotRenderer;

public sealed class ItemSlotRendererSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ItemSlotsSystem _slot = default!;

    private IGameTiming? _timing = null;
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemSlotRendererComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ItemSlotRendererComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ItemSlotRendererComponent, ContainerIsInsertingAttemptEvent>(OnInsertIntoContainer);
        SubscribeLocalEvent<ItemSlotRendererComponent, ContainerIsRemovingAttemptEvent>(OnRemoveFromContainer);

    }
    private void OnInsertIntoContainer(EntityUid uid, ItemSlotRendererComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        _timing ??= IoCManager.Resolve<IGameTiming>();
        if (args.Container is not ContainerSlot || !_timing.IsFirstTimePredicted)
            return;

        comp.CachedEntities[args.Container.ID] = args.EntityUid;
    }

    private void OnRemoveFromContainer(EntityUid uid, ItemSlotRendererComponent comp, ContainerIsRemovingAttemptEvent args)
    {
        _timing ??= IoCManager.Resolve<IGameTiming>();
        if (args.Container is not ContainerSlot || !_timing.IsFirstTimePredicted)
            return;

        comp.CachedEntities[args.Container.ID] = null;
    }

    private void OnRemove(EntityUid uid, ItemSlotRendererComponent comp, ComponentRemove args)
    {
        foreach (var (_, renderTexture) in comp.CachedRT)
            renderTexture.Dispose();
    }

    private void OnStartup(EntityUid uid, ItemSlotRendererComponent comp, ComponentStartup args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
        {
            Log.Error($"ItemSlotRendererCompontn requires SpriteComponent to work, but {ToPrettyString(uid)} did not have one. Removing ItemSlotRenderer.");
            RemComp<ItemSlotRendererComponent>(uid);
            return;
        }

        foreach (var kvp in comp.PrototypeLayerMappings)
        {

            (object mapKey, string slotId) = kvp;

            if (_reflection.TryParseEnumReference(kvp.Key, out var e, false))
                mapKey = e;

            if (sprite.LayerMapTryGet(mapKey, out _) && !comp.IgnoreMissing)
            {
                Log.Warning($"ItemSlotRenderer: Tried to add a missing layer under the key {mapKey}. Skipping missing layer. If this is unwanted, set component's AddMissingLayers to true.");
                continue;
            }

            if(_slot.TryGetSlot(uid, slotId, out var slot))
                comp.CachedEntities[slotId] = slot.Item;

            comp.LayerMappings.Add((mapKey, slotId));

            comp.CachedRT.Add(slotId, _clyde.CreateRenderTarget(comp.RenderTargetSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), new TextureSampleParameters { Filter = false }, $"{slotId}-itemrender-rendertarget"));
        }
    }

    [Dependency] private readonly IClyde _clyde = default!;

    //public override void FrameUpdate(float frameTime)
    //{
    //    var query = EntityQueryEnumerator<ItemSlotRendererComponent, SpriteComponent>();
    //    while(query.MoveNext(out var uid, out var comp, out var sprite))
    //    {
    //        for(int i = 0; i < comp.LayerMappings.Count; i++)
    //        {
    //            var (key, slotId) = comp.LayerMappings[i];
    //            if (comp.CachedEntities[slotId] is not EntityUid item ||
    //                !TryComp<SpriteComponent>(item, out var itemSprite))
    //                continue;
    //
    //        }
    //    }
    //}
}



public sealed class SpriteToLayerBullshitOverlay : Overlay
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly EntityManager _entMan = default!;


    public override OverlaySpace Space => OverlaySpace.ScreenSpaceBelowWorld;
    //private readonly Font _font;

    public SpriteToLayerBullshitOverlay()
    {
        IoCManager.InjectDependencies(this);
        //ZIndex = int.MinValue;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var query = _entMan.EntityQueryEnumerator<ItemSlotRendererComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            for (int i = 0; i < comp.LayerMappings.Count; i++)
            {
                var (key, slotId) = comp.LayerMappings[i];
                if (!comp.CachedRT.TryGetValue(slotId, out var renderTarget) ||
                    !comp.CachedEntities.TryGetValue(slotId, out var _item) ||
                    _item is not EntityUid item ||
                    !_entMan.TryGetComponent<SpriteComponent>(item, out var itemSprite))
                {
                    sprite.LayerSetTexture(sprite.LayerMapGet(key), Texture.Transparent);
                    continue;
                }

                handle.RenderInRenderTarget(renderTarget, () =>
                {
                    //sprite.Render(handle, 0, 0, null, _renderBackbuffer.Size / 2);
                    handle.DrawEntity(item, renderTarget.Size / 2, Vector2.One, 0);
                }, Color.Transparent);

                sprite.LayerSetTexture(sprite.LayerMapGet(key), renderTarget.Texture);
            }
        }

        /*
        var mouseScreenPos = _input.MouseScreenPosition.Position;

        var mouseMapPos = _eye.ScreenToMap(mouseScreenPos);
        // Why do i have to do so much to simply convert Vector2 from screenspace to worldspace and back?
        var finalMapPos = _hands.GetFinalDropCoordinates(player, _transform.GetMapCoordinates(player), mouseMapPos);
        var finalScreenPos = _eye.MapToScreen(new MapCoordinates(finalMapPos, mouseMapPos.MapId)).Position;

        var adjustedAngle = dropcomp.Angle;
        handle.RenderInRenderTarget(_renderBackbuffer, () =>
        {
            handle.DrawEntity(held, _renderBackbuffer.Size / 2, new Vector2(2), adjustedAngle);
        }, Color.Transparent);

        handle.DrawTexture(_renderBackbuffer.Texture, finalScreenPos - _renderBackbuffer.Size / 2, Color.GreenYellow.WithAlpha(0.75f));
        */
    }
}
