using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.UI;

[Serializable, NetSerializable]
public enum CyberwareAutodocUiKey : byte
{
    Key
}

/// <summary>
///     Данные доступного импланта для отображения в UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class AvailableImplantData
{
    public NetEntity Entity;
    public string Name;
    public CyberwareCategory Category;
    public float HumanityCost;

    public AvailableImplantData(NetEntity entity, string name, CyberwareCategory category, float humanityCost)
    {
        Entity = entity;
        Name = name;
        Category = category;
        HumanityCost = humanityCost;
    }
}

/// <summary>
///     Сетевое состояние Автодока: пациент, установленные импланты, доступные импланты рядом.
/// </summary>
[Serializable, NetSerializable]
public sealed class AutodocBoundUserInterfaceState : BoundUserInterfaceState
{
    public NetEntity? Patient;
    public Dictionary<CyberwareSlot, NetEntity> InstalledImplants;
    public List<AvailableImplantData> AvailableImplants;

    public AutodocBoundUserInterfaceState(
        NetEntity? patient,
        Dictionary<CyberwareSlot, NetEntity> installedImplants,
        List<AvailableImplantData> availableImplants)
    {
        Patient = patient;
        InstalledImplants = installedImplants;
        AvailableImplants = availableImplants;
    }
}

/// <summary>
///     Сообщение: "Интегрировать имплант в указанный слот".
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
///     Сообщение: "Извлечь имплант из указанного слота".
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