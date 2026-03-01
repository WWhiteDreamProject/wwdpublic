using Content.Shared._NC.Weapons.Ranged.NCWeapon;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._NC.Weapons.Ranged.Systems;

/// <summary>
/// Событие DoAfter для расклинивания оружия.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class UnjamWeaponDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Событие DoAfter для починки оружия гаечным ключом.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class WeaponRepairDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Shared-система: блокировка выстрела при заклинивании/поломке.
/// Работает на клиенте и сервере для корректной предикции звуков.
/// Используем PopupClient — показывает попап только на клиенте (без дубля от сервера).
/// </summary>
public sealed class SharedNCWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    // Cooldown попапов: не спамить «заклинило» каждый кадр при автострельбе
    private readonly Dictionary<EntityUid, TimeSpan> _lastJamPopup = new();
    private static readonly TimeSpan JamPopupCooldown = TimeSpan.FromSeconds(2);

    public override void Initialize()
    {
        base.Initialize();

        // Блокировка выстрела при заклинивании/поломке (shared — для предикции)
        SubscribeLocalEvent<NCWeaponComponent, ShotAttemptedEvent>(OnShotAttempted);

        // Расклинивание через UseInHand (кнопка Z / активация в руке)
        SubscribeLocalEvent<NCWeaponComponent, UseInHandEvent>(OnUseInHand);

        // Расклинивание через Alt-verb
        SubscribeLocalEvent<NCWeaponComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        // Завершение DoAfter расклинивания
        SubscribeLocalEvent<NCWeaponComponent, UnjamWeaponDoAfterEvent>(OnUnjamDoAfter);

        // Починка оружия гаечным ключом
        SubscribeLocalEvent<NCWeaponComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<NCWeaponComponent, WeaponRepairDoAfterEvent>(OnRepairDoAfter);
    }

    /// <summary>
    /// Блокировка выстрела при заклинивании или поломке.
    /// Попап с cooldown 2 сек, чтобы не спамить при автострельбе.
    /// </summary>
    private void OnShotAttempted(EntityUid uid, NCWeaponComponent component, ref ShotAttemptedEvent args)
    {
        // Оружие сломано — стрелять нельзя
        if (component.IsBroken)
        {
            args.Cancel();
            TryShowJamPopup(uid, args.User, Loc.GetString("nc-weapon-broken", ("weapon", uid)));
            return;
        }

        // Оружие заклинило — нужно расклинить
        if (component.IsJammed)
        {
            args.Cancel();
            TryShowJamPopup(uid, args.User, Loc.GetString("nc-weapon-jammed", ("weapon", uid)));
        }
    }

    /// <summary>
    /// Показать попап с cooldown — не чаще раз в 2 секунды на одно оружие.
    /// PopupClient — только на клиенте, без дубля от сервера.
    /// </summary>
    private void TryShowJamPopup(EntityUid uid, EntityUid user, string message)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;
        if (_lastJamPopup.TryGetValue(uid, out var last) && now - last < JamPopupCooldown)
            return;

        _lastJamPopup[uid] = now;
        _popup.PopupClient(message, uid, user);
    }

    /// <summary>
    /// Расклинивание через UseInHand (кнопка Z / активация в руке).
    /// Запускает DoAfter с задержкой.
    /// </summary>
    private void OnUseInHand(EntityUid uid, NCWeaponComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!component.IsJammed)
            return;

        args.Handled = true;
        StartUnjam(uid, args.User, component);
    }

    /// <summary>
    /// Alt-verb «Расклинить» — появляется только когда оружие заклинило.
    /// </summary>
    private void OnGetVerbs(EntityUid uid, NCWeaponComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!component.IsJammed)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("nc-weapon-verb-unjam"),
            Act = () => StartUnjam(uid, args.User, component),
            Priority = 10
        });
    }

    /// <summary>
    /// Запуск DoAfter для расклинивания.
    /// </summary>
    private void StartUnjam(EntityUid uid, EntityUid user, NCWeaponComponent component)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            TimeSpan.FromSeconds(component.UnjamTime),
            new UnjamWeaponDoAfterEvent(),
            uid,       // target
            uid)       // used
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            // PopupClient — без дубля от сервера
            _popup.PopupClient(
                Loc.GetString("nc-weapon-unjamming", ("weapon", uid)),
                uid,
                user);
        }
    }

    /// <summary>
    /// Завершение расклинивания после DoAfter.
    /// </summary>
    private void OnUnjamDoAfter(EntityUid uid, NCWeaponComponent component, UnjamWeaponDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!component.IsJammed)
            return;

        args.Handled = true;
        component.IsJammed = false;
        Dirty(uid, component);

        // Сбрасываем cooldown попапа
        _lastJamPopup.Remove(uid);

        // PopupClient — без дубля от сервера
        _popup.PopupClient(
            Loc.GetString("nc-weapon-unjammed", ("weapon", uid)),
            uid,
            args.User);
    }

    /// <summary>
    /// Изменяет время досылания патрона (у нас есть доступ через атрибут Access).
    /// </summary>
    public void SetBallisticFillDelay(EntityUid uid, Content.Shared.Weapons.Ranged.Components.BallisticAmmoProviderComponent component, TimeSpan delay)
    {
        component.FillDelay = delay;
        Dirty(uid, component);
    }

    /// <summary>
    /// Использование инструментов на оружии (починка гаечным ключом).
    /// </summary>
    private void OnInteractUsing(EntityUid uid, NCWeaponComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Можно ли вообще починить это оружие?
        if (!component.IsRepairable)
        {
            _popup.PopupClient(
                Loc.GetString("nc-weapon-repair-impossible", ("weapon", uid)),
                uid,
                args.User);
            return;
        }

        // Оружие уже полностью исправно
        if (component.Durability >= component.MaxDurability && !component.IsBroken)
        {
            _popup.PopupClient(
                Loc.GetString("nc-weapon-repair-full", ("weapon", uid)),
                uid,
                args.User);
            return;
        }

        // Проверяем, есть ли у предмета в руке свойства инструмента
        if (!TryComp<ToolComponent>(args.Used, out var tool))
            return;

        // Запускаем UseTool: требуем качество "Anchoring" (Wrench), задержка 5 секунд (базовая)
        var repairDelay = 5f;
        var hasTool = _tool.UseTool(
            args.Used,
            args.User,
            uid,
            repairDelay,
            new[] { "Anchoring" },
            new WeaponRepairDoAfterEvent()
        );

        if (hasTool)
        {
            args.Handled = true;
            _popup.PopupClient(
                Loc.GetString("nc-weapon-repair-start", ("weapon", uid)),
                uid,
                args.User);
        }
    }

    /// <summary>
    /// Завершение DoAfter починки оружия.
    /// Восстанавливает прочность до максимума и снимает поломку.
    /// </summary>
    private void OnRepairDoAfter(EntityUid uid, NCWeaponComponent component, WeaponRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        component.Durability = component.MaxDurability;
        component.IsBroken = false;
        Dirty(uid, component);

        _popup.PopupClient(
            Loc.GetString("nc-weapon-repair-finish", ("weapon", uid)),
            uid,
            args.User);
    }
}
