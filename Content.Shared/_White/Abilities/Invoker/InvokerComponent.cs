using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared._White.Abilities.Invoker;

[Serializable, NetSerializable]
public enum OrbType : byte
{
    Quas,
    Wex,
    Exort
}

[Serializable, NetSerializable]
public enum InvokerSpriteLayers : byte
{
    OrbSlot1 = 0,
    OrbSlot2 = 1,
    OrbSlot3 = 2
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InvokerComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<OrbType> CurrentOrbs = new();

    [DataField]
    public string? InvokedSpellPrototypeId;

    [DataField]
    public string OrbLayerMap = "invoker_orbs";

    [DataField]
    public SpriteSpecifier QuasSprite = new SpriteSpecifier.Rsi(
        new ResPath("/Textures/_White/Objects/Specific/invokerorbs.rsi"),
        "quas"
    );

    [DataField]
    public SpriteSpecifier WexSprite = new SpriteSpecifier.Rsi(
        new ResPath("/Textures/_White/Objects/Specific/invokerorbs.rsi"),
        "wex"
    );

    [DataField]
    public SpriteSpecifier ExortSprite = new SpriteSpecifier.Rsi(
        new ResPath("/Textures/_White/Objects/Specific/invokerorbs.rsi"),
        "exort"
    );

    [DataField]
    public List<Vector2> OrbOffsets = new()
    {
        new Vector2(-1, 0),
        new Vector2(0, 0),
        new Vector2(1, 0)
    };
}
