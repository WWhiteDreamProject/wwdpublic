using Robust.Shared.Serialization;

namespace Content.Shared._NC.Crafting.ConsoleCompiler;

// ─── BUI State: полные данные для отрисовки клиентского UI ───

/// <summary>
/// BUI State консоли-компилятора: все данные для UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsoleCompilerBoundUiState : BoundUserInterfaceState
{
    /// <summary>Текущий баланс данных в памяти консоли.</summary>
    public int AvailableData { get; }

    /// <summary>Стоимость печати чертежа.</summary>
    public int BlueprintCost { get; }

    /// <summary>Стоимость печати рецепта.</summary>
    public int RecipeCost { get; }

    /// <summary>Есть ли предмет в слоте Receiver.</summary>
    public bool HasReceiverItem { get; }

    /// <summary>Есть ли мастер-диск в слоте MasterDisk.</summary>
    public bool HasMasterDisk { get; }

    /// <summary>Отображаемое имя загруженной технологии.</summary>
    public string MasterDiskName { get; }

    /// <summary>Оставшиеся использования мастер-диска.</summary>
    public int MasterDiskUsesLeft { get; }

    /// <summary>Идёт ли процесс печати.</summary>
    public bool IsPrinting { get; }

    public ConsoleCompilerBoundUiState(
        int availableData,
        int blueprintCost,
        int recipeCost,
        bool hasReceiverItem,
        bool hasMasterDisk,
        string masterDiskName,
        int masterDiskUsesLeft,
        bool isPrinting)
    {
        AvailableData = availableData;
        BlueprintCost = blueprintCost;
        RecipeCost = recipeCost;
        HasReceiverItem = hasReceiverItem;
        HasMasterDisk = hasMasterDisk;
        MasterDiskName = masterDiskName;
        MasterDiskUsesLeft = masterDiskUsesLeft;
        IsPrinting = isPrinting;
    }
}

// ─── BUI Messages: команды от клиента к серверу ───

/// <summary>
/// Оцифровать предмет из ReceiverSlot: уничтожить RawData, прибавить DataPoints.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsoleCompilerDigitizeMessage : BoundUserInterfaceMessage { }

/// <summary>
/// Извлечь предмет из ReceiverSlot.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsoleCompilerEjectReceiverMessage : BoundUserInterfaceMessage { }

/// <summary>
/// Извлечь мастер-диск из MasterDiskSlot.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsoleCompilerEjectMasterMessage : BoundUserInterfaceMessage { }

/// <summary>
/// Запустить печать чертежа или рецепта.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsoleCompilerPrintMessage : BoundUserInterfaceMessage
{
    /// <summary>true = чертеж (Blueprint/Module), false = рецепт (Recipe).</summary>
    public bool IsBlueprint { get; }

    public ConsoleCompilerPrintMessage(bool isBlueprint)
    {
        IsBlueprint = isBlueprint;
    }
}
