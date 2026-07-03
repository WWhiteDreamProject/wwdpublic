using System.Collections.Immutable;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Client.IconSmoothing;

/// <summary>
///     Makes sprites of other grid-aligned entities like us connect.
/// </summary>
/// <remarks>
///     The system is based on Baystation12's smoothwalling, and thus will work with those.
///     To use, set <c>base</c> equal to the prefix of the corner states in the sprite base RSI.
///     Any objects with the same <c>key</c> will connect.
/// </remarks>
[RegisterComponent]
public sealed partial class IconSmoothComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public ImmutableArray<EdgeLayer> SmoothEdgeLayers;

    // a GridCoordinates struct would be nice here
    public (EntityUid, Vector2i)? GridPosition = null;

    /// <summary>
    ///     We will smooth with other objects with the same key.
    /// </summary>
    [DataField("key")]
    public string? SmoothKey { get; private set; }

    // TODO: make this accept a list of strings for entities that require smoothing multiple layers 
    /// <summary>
    ///     Layer key that will be smoothed. If not specified, the topmost layer will be used. 
    /// </summary>
    [DataField("layerKey")]
    public string? SpriteLayerStringKey;

    // TODO: make this into a List<object>?
    [ViewVariables(VVAccess.ReadOnly)]
    public object? SpriteLayerKey = null;

    /// <summary>
    ///     A list of keys to smooth with. If null, will default to smoothing with entities that have the same SmoothKey.
    /// </summary>
    [DataField]
    public HashSet<string>? MatchKeys = null;

    /// <summary>
    ///     A list of keys for edge layers to smooth with. If null, will default to smoothing with entities that have the same SmoothKey.
    /// </summary>
    [DataField]
    public HashSet<string>? EdgeMatchKeys = null;

    /// <summary>
    ///     Prepended to the RSI state.
    /// </summary>
    [DataField("base")]
    public string StateBase { get; set; } = string.Empty;

    [DataField(customTypeSerializer:typeof(PrototypeIdSerializer<ShaderPrototype>))]
    public string? Shader;

    /// <summary>
    ///     Mode that controls how the icon should be selected.
    /// </summary>
    [DataField]
    public IconSmoothingMode Mode = IconSmoothingMode.Corners;

    /// <summary>
    ///     Used by <see cref="IconSmoothSystem"/> to reduce redundant updates.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    internal int UpdateGeneration { get; set; }

    /// <summary>
    /// By default, edge layers are hidden if there is a matching entity in its direction.
    /// If this is true, this behaviour is inverted.
    /// </summary>
    [DataField]
    public bool ShowEdgeIfMatching = false;
}

/// <summary>
///     Controls the mode with which icon smoothing is calculated.
/// </summary>
[PublicAPI]
public enum IconSmoothingMode : byte
{
    /// <summary>
    ///     Each icon is made up of 4 corners, each of which can get a different state depending on
    ///     adjacent entities clockwise, counter-clockwise and diagonal with the corner.
    /// </summary>
    Corners,

    /// <summary>
    ///     There are 16 icons, only one of which is used at once.
    ///     The icon selected is a bit field made up of the cardinal direction flags that have adjacent entities.
    /// </summary>
    CardinalFlags,

    /// <summary>
    ///     The icon represents a triangular sprite with only 2 states, representing South / East being occupied or not.
    /// </summary>
    Diagonal,

    /// <summary>
    ///     Uses same icon state format as <see cref="CardinalFlags"/>.
    ///     Will only connect entities to the *left* and *right* of this entity. (As opposed to east and west) 
    /// </summary>
    Horizontal,

    /// <summary>
    ///     Uses same icon state format as <see cref="CardinalFlags"/>.
    ///     Will only connect entities to the *front* and *back* of this entity. (As opposed to north and south) 
    /// </summary>
    Vertical,

    /// <summary>
    ///     Where this component contributes to our neighbors being calculated but we do not update our own sprite.
    /// </summary>
    NoSprite,
}

public enum EdgeLayer : byte
{
    None = 0, // whoever uses this as a layer map key will be shot on sight, this only exists as a crutch to allow using this enum as with bitfields so i don't have to make a second "EdgeLayerBitfield" enum 
    South = 1 << 0,
    East = 1 << 1,
    North = 1 << 2,
    West = 1 << 3,

    SouthEast = 1 << 4,
    NorthEast = 1 << 5,
    NorthWest = 1 << 6,
    SouthWest = 1 << 7,
}