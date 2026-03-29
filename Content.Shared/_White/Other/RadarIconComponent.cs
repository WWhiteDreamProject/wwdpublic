using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._White.Other;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RadarIconComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<List<Vector2>> Lines = new();

    [DataField, AutoNetworkedField]
    public List<List<Vector2>> LinesNoRot = new();

    /// <summary>
    /// How close an entity has to be for the icon to appear on radar.
    /// 0 to disable.
    /// </summary>
    [DataField, AutoNetworkedField, Animatable]
    public double RadarRange {get; set;} = 0;

    [DataField, AutoNetworkedField, Animatable]
    public Color Color {get; set;} = Color.Silver;

    [DataField, AutoNetworkedField, Animatable]
    public Angle Angle {get; set;} = 0;

    [DataField, AutoNetworkedField, Animatable]
    public float Scale {get; set;} = 1;

    [DataField, AutoNetworkedField, Animatable]
    public bool ShowOnGrid {get; set;} = false;
}
