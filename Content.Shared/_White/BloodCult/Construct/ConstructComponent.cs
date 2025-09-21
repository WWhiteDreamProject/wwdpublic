using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.BloodCult.Construct;

[RegisterComponent]
public sealed partial class ConstructComponent : Component
{
    /// <summary>
    ///     Used by the client to determine how long the transform animation should be played.
    /// </summary>
    [DataField]
    public float TransformDelay = 1;

    [DataField]
    public ProtoId<EntityPrototype> SpawnOnDeathPrototype { get; set; } = "Ectoplasm";

    public bool Transforming = false;
    public float TransformAccumulator = 0;
}

[Serializable, NetSerializable]
public enum ConstructVisualsState : byte
{
    Transforming,
    Sprite,
    Glow
}
