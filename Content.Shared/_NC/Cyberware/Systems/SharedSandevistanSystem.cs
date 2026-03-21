using Content.Shared._NC.Cyberware.Components;
using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.Systems;

public abstract class SharedSandevistanSystem : EntitySystem
{
    [Dependency] protected readonly MovementSpeedModifierSystem MovementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveSandevistanComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        // Используем кастомный или существующий ивент для модификации скорости атаки
        SubscribeLocalEvent<ActiveSandevistanComponent, MeleeWeaponRefreshModifiersEvent>(OnRefreshMelee);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, ActiveSandevistanComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<SandevistanComponent>(component.ImplantEntity, out var sande))
            return;

        args.ModifySpeed(sande.SpeedMultiplier, sande.SpeedMultiplier);
    }

    private void OnRefreshMelee(EntityUid uid, ActiveSandevistanComponent component, ref MeleeWeaponRefreshModifiersEvent args)
    {
        if (!TryComp<SandevistanComponent>(component.ImplantEntity, out var sande))
            return;

        args.AttackRate *= sande.AttackSpeedMultiplier;
    }

    public virtual void ToggleSandevistan(EntityUid user, EntityUid implant, SandevistanComponent? component = null)
    {
    }
}

/// <summary>
///     Ивент для экшена включения Сандевистана.
/// </summary>
public sealed partial class SandevistanToggleActionEvent : InstantActionEvent { }

/// <summary>
///     Ивент обновления параметров ближнего боя. 
///     Если он уже есть в проекте, это объявление может вызвать конфликт.
/// </summary>
[ByRefEvent]
public struct MeleeWeaponRefreshModifiersEvent
{
    public float AttackRate = 1f;

    public MeleeWeaponRefreshModifiersEvent()
    {
    }
}
