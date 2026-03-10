using Content.Shared.Containers.ItemSlots;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Crafting.ConsoleCompiler;

/// <summary>
/// Компонент консоли-компилятора (Техно-Принтер).
/// Принимает RawData для пополнения баланса данных
/// и DecryptionTechnology для печати чертежей/рецептов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsoleCompilerComponent : Component
{
    // ─── Идентификаторы слотов ───

    public const string ReceiverSlotId = "compiler_receiver_slot";
    public const string MasterDiskSlotId = "compiler_master_disk_slot";

    // ─── Слоты (ItemSlotsSystem) ───

    /// <summary>
    /// Приёмник данных — принимает энтити с RawDataComponent (для оцифровки).
    /// </summary>
    [DataField("receiverSlot")]
    public ItemSlot ReceiverSlot = new()
    {
        Name = "Receiver",
        Whitelist = new EntityWhitelist
        {
            Components = new[] { "RawData" }
        }
    };

    /// <summary>
    /// Слот мастер-диска — принимает энтити с DecryptionTechnology (для печати).
    /// </summary>
    [DataField("masterDiskSlot")]
    public ItemSlot MasterDiskSlot = new()
    {
        Name = "Master Disk",
        Whitelist = new EntityWhitelist
        {
            Components = new[] { "DecryptionTechnology" }
        }
    };

    // ─── Баланс и стоимости ───

    /// <summary>
    /// Текущий баланс очков данных в памяти консоли.
    /// </summary>
    [DataField("availableData"), AutoNetworkedField]
    public int AvailableData;

    /// <summary>
    /// Стоимость печати чертежа (Blueprint / Module).
    /// </summary>
    [DataField("printBlueprintCost"), AutoNetworkedField]
    public int PrintBlueprintCost = 400;

    /// <summary>
    /// Стоимость печати рецепта (Recipe).
    /// </summary>
    [DataField("printRecipeCost"), AutoNetworkedField]
    public int PrintRecipeCost = 100;

    /// <summary>
    /// Длительность печати в секундах (прогресс-бар DoAfter).
    /// </summary>
    [DataField("printDoAfterTime")]
    public float PrintDoAfterTime = 4.0f;

    /// <summary>
    /// Прототип сгоревшей болванки, спавнится при истощении мастер-диска.
    /// </summary>
    [DataField("burnedDiskPrototype")]
    public string BurnedDiskPrototype = "NCRawDataBurned";

    /// <summary>
    /// Флаг: идёт ли сейчас процесс печати (блокирует повторный запуск).
    /// </summary>
    [AutoNetworkedField]
    public bool IsPrinting;
}

/// <summary>
/// UI-ключ для BUI консоли-компилятора.
/// </summary>
[Serializable, NetSerializable]
public enum ConsoleCompilerUiKey : byte
{
    Key
}
