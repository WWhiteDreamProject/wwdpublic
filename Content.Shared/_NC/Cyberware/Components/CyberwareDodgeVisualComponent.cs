using System.Numerics;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Добавляется временно при уклонении для отрисовки рывка на клиенте.
///     Не синхронизируется по сети, создается клиентской системой при получении события.
/// </summary>
[RegisterComponent]
public sealed partial class CyberwareDodgeVisualComponent : Component
{
    [DataField("direction")]
    public Vector2 Direction;

    [DataField("accumulator")]
    public float Accumulator = 0f;

    [DataField("lifetime")]
    public float Lifetime = 0.2f;
}
