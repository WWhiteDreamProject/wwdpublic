using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.UI;

[Serializable, NetSerializable]
public enum CyberwareAutodocUiKey : byte
{
    Key
}

/// <summary>
///     Сетевое состояние, отправляемое с сервера на клиент Автодока (Рипердоку), 
///     чтобы отобразить лежащего пациента и его доступные/занятые слоты.
/// </summary>
[Serializable, NetSerializable]
public sealed class AutodocBoundUserInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    ///     Uid пациента, лежащего внутри капсулы (для SpriteView).
    /// </summary>
    public NetEntity? Patient;

    /// <summary>
    ///     Словарь установленных имплантов (Слот -> Uid и Имя импланта).
    /// </summary>
    public Dictionary<CyberwareSlot, NetEntity> InstalledImplants;

    public AutodocBoundUserInterfaceState(NetEntity? patient, Dictionary<CyberwareSlot, NetEntity> installedImplants)
    {
        Patient = patient;
        InstalledImplants = installedImplants;
    }
}

/// <summary>
///     Сообщение от клиента (BUI) к серверу с командой: "Интегрировать имплант".
/// </summary>
[Serializable, NetSerializable]
public sealed class AutodocInstallBuiMsg : BoundUserInterfaceMessage
{
    public NetEntity ImplantEntity;
    public CyberwareSlot Slot;

    public AutodocInstallBuiMsg(NetEntity implantEntity, CyberwareSlot slot)
    {
        ImplantEntity = implantEntity;
        Slot = slot;
    }
}

/// <summary>
///     Сообщение от клиента (BUI) к серверу с командой: "Извлечь имплант".
/// </summary>
[Serializable, NetSerializable]
public sealed class AutodocRemoveBuiMsg : BoundUserInterfaceMessage
{
    public CyberwareSlot Slot;

    public AutodocRemoveBuiMsg(CyberwareSlot slot)
    {
        Slot = slot;
    }
}