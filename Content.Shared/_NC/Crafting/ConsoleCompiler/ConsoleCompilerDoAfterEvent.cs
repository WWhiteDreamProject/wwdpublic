using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Crafting.ConsoleCompiler;

/// <summary>
/// Ивент для прогрессбара (DoAfter) при печати чертежа/рецепта на консоли-компиляторе.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ConsoleCompilerDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// true = печать чертежа (Blueprint/Module), false = печать рецепта (Recipe).
    /// </summary>
    public bool IsBlueprint { get; }

    public ConsoleCompilerDoAfterEvent(bool isBlueprint)
    {
        IsBlueprint = isBlueprint;
    }
}
