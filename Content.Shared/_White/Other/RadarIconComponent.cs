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
    /// List of lines. See <see cref="RadarIconLineDefinition"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<RadarIconLineDefinition> Lines = new();

    /// <summary>
    /// How close an entity has to be for the icon to appear on radar.
    /// 0 to disable.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public double RadarRange { get; set; } = 0;
    
    /// <summary>
    /// Icon color. Used if a line doesn't have it's own color specified.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Color Color { get; set; } = Color.Silver;

    /// <summary>
    /// Rotates the entire icon clockwise.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Angle Angle { get; set; } = Angle.Zero;

    /// <summary>
    /// Offset that is applied to the entire icon.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Scales the entire icon.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// If true, the icon will appear even if entity is standing on a grid.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public bool ShowOnGrid { get; set; } = false;

    /// <summary>
    /// If true, the icon size will be affected by radar zoom. If false, the icon will stay constant size. 
    /// </summary>

    [DataField, Animatable]
    public bool ConstantSize { get; set; } = true;

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
    /// Offset that is applied to this line.
    /// </summary>
    [DataField, Animatable]
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Rotation applied to this line.
    /// </summary>
    [DataField, Animatable]
    public Angle Angle { get; set; } = Angle.Zero;

    /// <summary>
    /// Scaling applied to this line (before this line's offset)
    /// </summary>
    [DataField, Animatable]
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// If false, the line will follow the entity rotation.
    /// </summary>
    [DataField("noRot"), Animatable]
    public bool NoRotation { get; set; } = false;

    /// <summary>
    /// Line color. If null, will use the component color.
    /// </summary>
    [DataField, Animatable]
    public Color? Color { get; set; } = null;

    /// <summary>
    /// How the points will be drawn.
    /// Default is LineStrip.
    /// </summary>
    [DataField]
    public DrawModeEnum DrawMode = DrawModeEnum.LineStrip;


    /// <summary>
    /// These match DrawPrimitiveTopology enum from Robust.Client.Graphics, which, in turn, match OpenGL primitive rendering modes.
    /// See  https://wikis.khronos.org/opengl/Primitive or <see href="https://www.khronos.org/registry/vulkan/specs/1.2-extensions/html/vkspec.html#drawing-point-lists">Vulkan's documentation</see>
    /// Relevant citations from the first page page are also provided in each value summary.
    /// </summary>
    public enum DrawModeEnum : byte
    {
        /// <summary>
        /// Doesn't seem to be relevant in context of SS14.
        /// </summary>
        PointList,
        /// <summary>
        /// Vertices 0, 1, and 2 form a triangle. Vertices 3, 4, and 5 form a triangle. And so on.
        /// </summary>
        TriangleList,
        /// <summary>
        /// The first vertex is always held fixed. From there on, every group of 2 adjacent vertices
        /// form a triangle with the first. So with a vertex stream, you get a list of triangles
        /// like so: (0, 1, 2) (0, 2, 3), (0, 3, 4), etc. A vertex stream of n length will generate n-2 triangles.
        /// </summary>
        TriangleFan,
        /// <summary>
        /// Every group of 3 adjacent vertices forms a triangle.
        /// A vertex stream of n length will generate n-2 triangles.
        /// </summary>
        TriangleStrip,
        /// <summary>
        /// Vertices 0 and 1 are considered a line. Vertices 2 and 3 are considered a line. And so on.
        /// If the user specifies a non-even number of vertices, then the extra vertex is ignored.
        /// </summary>
        LineList,
        /// <summary>
        /// The adjacent vertices are considered lines. Thus, if you pass n vertices, you will get n-1 lines.
        /// If the user only specifies 1 vertex, the drawing command is ignored.
        /// </summary>
        LineStrip,
        /// <summary>
        /// As line strips, except that the first and last vertices are also used as a line.
        /// Thus, you get n lines for n input vertices. If the user only specifies 1 vertex,
        /// the drawing command is ignored. The line between the first and last vertices happens
        /// after all of the previous lines in the sequence.
        /// </summary>
        LineLoop
    }
}