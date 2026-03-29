using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._White.Other;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RadarIconComponent : Component
{

        /// <summary>
    /// List of lines that will rotate along with the entity.
    /// Format:
    /// - type: radarIcon
    ///   lines:
    ///   - [ # first line
    ///     "0, 0",
    ///     "0, 1",
    ///     # ...
    ///     "1, 0"
    ///   ]
    ///   - [  # second line
    ///     # ...
    ///   ]
    ///   # ...
    /// </summary>
    /// <remarks>
    /// Each line must have at least two points, and can have more.
    /// </remarks>
    /// <remarks>
    /// </remarks>
    [DataField, AutoNetworkedField]
    public List<RadarIconLineDefinition> Lines = new();

    /// <summary>
    /// How close an entity has to be for the icon to appear on radar.
    /// 0 to disable.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public double RadarRange { get; set; } = 0;
    
    /// <summary>
    /// Icon color.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Color Color { get; set; } = Color.Silver;

    /// <summary>
    /// Rotates the entire icon clockwise.
    /// The part that rotates with the entity *also* gets rotated, keep it in mind.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Angle Angle { get; set; } = Angle.Zero;

    /// <summary>
    /// Offset that is applied to the entire icon.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Vector2 Offset { get; set; } = Vector2.Zero;


    /// <summary>
    /// Icon scale.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// If true, the icon will appear even if entity is standing on a grid.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public bool ShowOnGrid { get; set; } = false;

}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RadarIconLineDefinition
{
    /// <summary>
    /// List of points. Must have at least two.
    /// </summary>
    [DataField(required: true)]
    public List<Vector2> Points = new();

    /// <summary>
    /// Translation applied to all points before rotation.
    /// </summary>
    [DataField, Animatable]
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Rotation applied to all points after translation.
    /// </summary>
    [DataField, Animatable]
    public Angle Angle { get; set; } = Angle.Zero;

    /// <summary>
    /// All points are multiplied by this in as well as the component scale value.
    /// </summary>
    [DataField, Animatable]
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// If false, follows the entity rotations.
    /// </summary>
    [DataField, Animatable]
    public bool NoRot { get; set; } = false;

    /// <summary>
    /// Line color. If null, value from component is used.
    /// </summary>
    [DataField, Animatable]
    public Color? Color { get; set; } = null;

}