using Robust.Shared.Prototypes;

namespace Content.Server._NC.StyleSheet;

/// <summary>
/// Заглушка для серверной части, чтобы сервер не падал при встрече прототипа 'styleSheet'.
/// Сами стили используются только на клиенте.
/// </summary>
[Prototype("styleSheet")]
public sealed class StyleSheetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}

/// <summary>
/// Заглушка для 'dynamicValue', если они тоже используются в стилях.
/// </summary>
[Prototype("dynamicValue")]
public sealed class DynamicValuePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
