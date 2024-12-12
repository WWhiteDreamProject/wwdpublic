using Content.Shared._White.Event;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.Event;

public class EventItemDispenserSystem : SharedEventItemDispenserSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EventItemDispenserComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EventItemDispenserComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);

    }

    private void OnInit(EntityUid uid, EventItemDispenserComponent comp, ComponentInit args)
    {
        var sprite = Comp<SpriteComponent>(uid);
        var icon = _sprite.GetPrototypeIcon(comp.DispensingPrototype).Default;
        sprite.LayerSetTexture(EventItemDispenserVisualLayers.ItemPreview, icon);
        float scale = comp.ItemPreviewScale;

        if(scale <= 0)
        {
            scale = 32f / Math.Max(15, Math.Max(icon.Width, icon.Height));
        }
        sprite.LayerSetScale(EventItemDispenserVisualLayers.ItemPreview, new Vector2(scale));
    }
    private void OnAfterAutoHandleState(EntityUid uid, EventItemDispenserComponent comp, AfterAutoHandleStateEvent args)
    {
        var sprite = Comp<SpriteComponent>(uid);
        var icon = _sprite.GetPrototypeIcon(comp.DispensingPrototype).Default;
        sprite.LayerSetTexture(EventItemDispenserVisualLayers.ItemPreview, icon);

        float scale = comp.ItemPreviewScale;
        if (scale <= 0)
        {
            scale = 32f / Math.Max(15, Math.Max(icon.Width, icon.Height));
        }
        sprite.LayerSetScale(EventItemDispenserVisualLayers.ItemPreview, new Vector2(scale));
    }
}

enum EventItemDispenserVisualLayers : byte
{
    Base,
    Lights,
    Arms,
    ItemPreview
}
