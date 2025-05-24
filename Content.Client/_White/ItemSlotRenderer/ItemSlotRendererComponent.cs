using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.ItemSlotRenderer;

[RegisterComponent]
public sealed partial class ItemSlotRendererComponent : Component
{
    // string layer mappings to slot ids
    [DataField]
    public Dictionary<string, string> PrototypeLayerMappings = new();

    // [mapkey] = slotId
    [ViewVariables(VVAccess.ReadWrite)]
    public List<(object, string)> LayerMappings = new();

    // [slotId] = entity uid
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, EntityUid?> CachedEntities = new();

    // [slotId] = IRenderTexture
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, IRenderTexture> CachedRT = new();

    [DataField]
    public bool IgnoreMissing = false;

    [DataField]
    public Vector2i RenderTargetSize = new Vector2i(32, 32);
}

/*
            if (reflection.TryParseEnumReference(keyString, out var @enum))
                return @enum;

            return keyString;
*/
