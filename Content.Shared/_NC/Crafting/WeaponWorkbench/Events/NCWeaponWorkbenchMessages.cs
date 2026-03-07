using Content.Shared._NC.Crafting.WeaponWorkbench.Components;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared._NC.Crafting.WeaponWorkbench.Events;

/// <summary>
/// BUI State: все данные для отрисовки клиентского интерфейса верстака.
/// </summary>
[Serializable, NetSerializable]
public sealed class NCWeaponWorkbenchUpdateState : BoundUserInterfaceState
{
    public NCWeaponWorkbenchState WorkbenchState { get; }
    public float Heat { get; }
    public float Integrity { get; }
    public float Alignment { get; }
    public float Progress { get; }
    public float SafeZoneHalfWidth { get; }
    public string WarningMessage { get; }
    public bool HasMaterial { get; }
    public bool IsFlashing { get; }              // Красное мигание при аномалии
    public float ButtonCooldownRemaining { get; } // Оставшийся кулдаун кнопок (0..0.5)
    public bool IsSystemLocked { get; }           // Системная блокировка (Тир 3)
    public string? LockCode { get; }              // 4-значный код для блокировки

    public NCWeaponWorkbenchUpdateState(
        NCWeaponWorkbenchState state,
        float heat,
        float integrity,
        float alignment,
        float progress,
        float safeZoneHalfWidth,
        string warningMessage,
        bool hasMaterial,
        bool isFlashing,
        float buttonCooldownRemaining,
        bool isSystemLocked,
        string? lockCode)
    {
        WorkbenchState = state;
        Heat = heat;
        Integrity = integrity;
        Alignment = alignment;
        Progress = progress;
        SafeZoneHalfWidth = safeZoneHalfWidth;
        WarningMessage = warningMessage;
        HasMaterial = hasMaterial;
        IsFlashing = isFlashing;
        ButtonCooldownRemaining = buttonCooldownRemaining;
        IsSystemLocked = isSystemLocked;
        LockCode = lockCode;
    }
}

/// <summary>
/// Команды оператора, передаваемые из UI на сервер.
/// </summary>
[Serializable, NetSerializable]
public enum OperatorCommandType : byte
{
    StartScraping,  // Запуск цикла обработки
    ApplyCoolant,   // Охладитель (Heat вниз)
    SpotWeld,       // Точечная сварка (Integrity вверх)
    AlignLeft,      // Калибровка влево
    AlignRight      // Калибровка вправо
}

/// <summary>
/// BUI Message: команда оператора от клиента к серверу.
/// </summary>
[Serializable, NetSerializable]
public sealed class NCWorkbenchOperatorCommandMessage : BoundUserInterfaceMessage
{
    public OperatorCommandType CommandType { get; }

    public NCWorkbenchOperatorCommandMessage(OperatorCommandType type)
    {
        CommandType = type;
    }
}

/// <summary>
/// BUI Message: ввод 4-значного кода для снятия системной блокировки (Тир 3).
/// </summary>
[Serializable, NetSerializable]
public sealed class NCWorkbenchLockCodeInputMessage : BoundUserInterfaceMessage
{
    public string Code { get; }

    public NCWorkbenchLockCodeInputMessage(string code)
    {
        Code = code;
    }
}
