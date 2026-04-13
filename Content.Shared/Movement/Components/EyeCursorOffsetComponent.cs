using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Displaces SS14 eye data when given to an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EyeCursorOffsetComponent : Component
{
    /// <summary>
    /// The amount the view will be displaced when the cursor is positioned at/beyond the max offset distance.
    /// Measured in tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxOffset = 3f;

    /// <summary>
    /// The speed which the camera adjusts to new positions. 0.5f seems like a good value, but can be changed if you want very slow/instant adjustments.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OffsetSpeed = 0.5f;

    /// <summary>
    /// The amount the PVS should increase to account for the max offset.
    /// Should be 1/10 of MaxOffset most of the time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PvsIncrease = 0.3f;

    /// <summary>
    /// Текущее смещение, синхронизированное с сервером.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2 CurrentOffset = Vector2.Zero;

    /// <summary>
    /// Целевое положение (только для клиента, для плавности).
    /// </summary>
    [ViewVariables]
    public Vector2 TargetPosition = Vector2.Zero;
}

/// <summary>
/// Событие для отправки смещения от клиента к серверу.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestEyeCursorOffsetEvent : EntityEventArgs
{
    public Vector2 Offset;

    public RequestEyeCursorOffsetEvent(Vector2 offset)
    {
        Offset = offset;
    }
}
