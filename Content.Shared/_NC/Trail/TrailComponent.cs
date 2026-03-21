using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;

namespace Content.Shared._NC.Trail;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TrailComponent : Component
{
    [DataField, AutoNetworkedField] public float Frequency = 0.05f;
    [DataField, AutoNetworkedField] public float Lifetime = 0.5f;
    [DataField, AutoNetworkedField] public Color Color = Color.White;
    [DataField, AutoNetworkedField] public float Scale = 1f;
    [DataField, AutoNetworkedField] public SpriteSpecifier? Sprite;
    [DataField, AutoNetworkedField] public EntProtoId? Shader;
    
    public List<TrailData> TrailData = new();
    public float Accumulator;
    
    [DataField, AutoNetworkedField] public EntityUid? RenderedEntity;
    [DataField, AutoNetworkedField] public RenderedEntityRotationStrategy RenderedEntityRotationStrategy = RenderedEntityRotationStrategy.Trail;

    [DataField, AutoNetworkedField] public float AlphaLerpAmount = 0f;
    [DataField, AutoNetworkedField] public float AlphaLerpTarget = 0f;
    [DataField, AutoNetworkedField] public float ScaleLerpAmount = 0f;
    [DataField, AutoNetworkedField] public float ScaleLerpTarget = 0f;
    [DataField, AutoNetworkedField] public float Velocity = 0f;
    [DataField, AutoNetworkedField] public float VelocityLerpAmount = 0f;
    [DataField, AutoNetworkedField] public float VelocityLerpTarget = 0f;
    [DataField, AutoNetworkedField] public float PositionLerpAmount = 0f;
    
    [DataField, AutoNetworkedField] public float LerpTime = 0.05f;
    public float LerpAccumulator;
    public TimeSpan LerpDelay = TimeSpan.Zero;

    [DataField, AutoNetworkedField] public int ParticleAmount = 1;
    [DataField, AutoNetworkedField] public int MaxParticleAmount = 0;
    public int ParticleCount = 0;
    public int CurIndex = 0;
    [DataField, AutoNetworkedField] public Angle StartAngle = Angle.Zero;
    [DataField, AutoNetworkedField] public Angle EndAngle = Angle.Zero;
    [DataField, AutoNetworkedField] public float Radius = 0f;
    [DataField, AutoNetworkedField] public Vector2? SpawnPosition;
    [DataField, AutoNetworkedField] public EntityUid? SpawnEntityPosition;
    [DataField, AutoNetworkedField] public bool SpawnRemainingTrail = true;
    
    public MapCoordinates LastCoords;
    public Dictionary<string, object> ShaderData = new();
    public List<AdditionalLerpData> AdditionalLerpData = new();
}

[Serializable, NetSerializable]
public sealed class TrailData
{
    public Vector2 Position;
    public float Velocity;
    public MapId MapId;
    public Vector2 Direction;
    public Angle Angle;
    public Color Color;
    public float Scale;
    public TimeSpan SpawnTime;

    public TrailData(Vector2 position, float velocity, MapId mapId, Vector2 direction, Angle angle, Color color, float scale, TimeSpan spawnTime)
    {
        Position = position;
        Velocity = velocity;
        MapId = mapId;
        Direction = direction;
        Angle = angle;
        Color = color;
        Scale = scale;
        SpawnTime = spawnTime;
    }
}

public enum RenderedEntityRotationStrategy : byte
{
    Trail,
    RenderedEntity,
    Particle
}

[Serializable, NetSerializable]
public sealed class AdditionalLerpData
{
    public string Property = "";
    public float Value;
    public float LerpTarget;
    public float LerpAmount;
}

public abstract class GetShaderData { }
public sealed class GetShaderLocalPositionData : GetShaderData { }
public sealed class GetShaderFloatParam : GetShaderData { public string Param = ""; }
