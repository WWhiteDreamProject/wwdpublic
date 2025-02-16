using Content.Shared._White;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.AntiParkinsons;

[RegisterComponent]
[UnsavedComponent]
public sealed partial class PixelSnapEyeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid LastParent;
    [ViewVariables(VVAccess.ReadWrite)]
    public System.Numerics.Vector2 SpriteOffset, SpriteOffsetModified;
    [ViewVariables(VVAccess.ReadWrite)]
    public MapCoordinates EyePosition, EyePositionModified;
    [ViewVariables(VVAccess.ReadWrite)]
    public System.Numerics.Vector2 EyeOffset, EyeOffsetModified;

}
