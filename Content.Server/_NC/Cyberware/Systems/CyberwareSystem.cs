using Content.Shared._NC.Cyberware;
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware.Systems;
using Content.Shared.DoAfter;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Content.Shared.Popups;

namespace Content.Server._NC.Cyberware.Systems;

/// <summary>
///     Основная система для установки и извлечения киберимплантов.
/// </summary>
public sealed class CyberwareSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly HumanitySystem _humanitySystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CyberwareComponent, ComponentInit>(OnCyberwareInit);
    }

    private void OnCyberwareInit(EntityUid uid, CyberwareComponent component, ComponentInit args)
    {
        // Гарантируем, что контейнер для имплантов создан при появлении компонента
        _container.EnsureContainer<Container>(uid, CyberwareComponent.ContainerName);
    }

    /// <summary>
    ///     Прямая установка импланта в цель (обычно вызывается после завершения DoAfter из Автодока).
    /// </summary>
    public bool TryInstallImplant(EntityUid target, EntityUid implant, CyberwareComponent? cyberware = null, CyberwareImplantComponent? implantComp = null)
    {
        if (!Resolve(target, ref cyberware, false) || !Resolve(implant, ref implantComp, false))
            return false;

        var slot = implantComp.Slot;

        // Проверяем, свободен ли слот
        if (cyberware.InstalledImplants.ContainsKey(slot))
            return false;

        var container = _container.GetContainer(target, CyberwareComponent.ContainerName);
        if (!_container.Insert(implant, container))
            return false;

        cyberware.InstalledImplants[slot] = implant;
        Dirty(target, cyberware);

        // Списываем человечность
        _humanitySystem.DeductHumanity(target, implantComp.HumanityCost);
        
        _popup.PopupEntity($"Имплант успешно интегрирован в слот {slot}.", target, PopupType.Medium);

        return true;
    }

    /// <summary>
    ///     Извлечение импланта из цели.
    /// </summary>
    public bool TryRemoveImplant(EntityUid target, CyberwareSlot slot, CyberwareComponent? cyberware = null)
    {
        if (!Resolve(target, ref cyberware, false))
            return false;

        if (!cyberware.InstalledImplants.TryGetValue(slot, out var implant) || Deleted(implant))
            return false;

        var container = _container.GetContainer(target, CyberwareComponent.ContainerName);
        if (!_container.Remove(implant, container))
            return false;

        cyberware.InstalledImplants.Remove(slot);
        Dirty(target, cyberware);

        // Рассчитываем возврат человечности и перманентную травму
        if (TryComp<CyberwareImplantComponent>(implant, out var implantComp))
        {
            float refund = implantComp.HumanityCost * implantComp.RefundPercentage;
            float trauma = implantComp.HumanityCost - refund;

            _humanitySystem.ReduceMaxHumanity(target, trauma);
            _humanitySystem.RestoreHumanity(target, refund);
        }

        _popup.PopupEntity($"Имплант успешно извлечен из слота {slot}.", target, PopupType.LargeCaution);

        return true;
    }
}