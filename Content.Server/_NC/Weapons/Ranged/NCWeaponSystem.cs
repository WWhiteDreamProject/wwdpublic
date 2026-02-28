using Content.Shared._NC.Weapons.Ranged.Events;
using Content.Shared._NC.Weapons.Ranged.NCWeapon;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Timing;

namespace Content.Server._NC.Weapons.Ranged;

/// <summary>
/// Серверная система обработки механик оружейных брендов Найт-Сити.
/// Подписывается на события стрельбы и применяет уникальные эффекты
/// в зависимости от производителя и тира оружия.
/// </summary>
public sealed class NCWeaponSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly Content.Shared._NC.Weapons.Ranged.Systems.SharedNCWeaponSystem _sharedNcWeapon = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Блокировка выстрела — в SharedNCWeaponSystem (для предикции на клиенте)

        // Инициализация модификаторов бренда при спавне оружия
        SubscribeLocalEvent<NCWeaponComponent, MapInitEvent>(OnMapInit);

        // Обработка после выстрела: деградация, перегрев, шанс заклинивания/взрыва
        SubscribeLocalEvent<NCWeaponComponent, GunShotEvent>(OnGunShot);

        // Модификация параметров оружия (разброс) — вызывается каждый RefreshModifiers
        SubscribeLocalEvent<NCWeaponComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);

        // Задержка при доставании оружия (DrawTimeMultiplier)
        SubscribeLocalEvent<NCWeaponComponent, Content.Shared.Hands.GotEquippedHandEvent>(OnEquipped);
    }

    /// <summary>
    /// При взятии оружия в руки запускаем задержку UseDelay (оружие нельзя использовать N секунд).
    /// </summary>
    private void OnEquipped(EntityUid uid, NCWeaponComponent component, ref Content.Shared.Hands.GotEquippedHandEvent args)
    {
        if (component.DrawTimeMultiplier > 1f)
        {
            _useDelay.TryResetDelay(uid, true);
        }
    }

    /// <summary>
    /// При спавне оружия — применяем модификаторы бренда к GunComponent и смежным.
    /// </summary>
    private void OnMapInit(EntityUid uid, NCWeaponComponent component, MapInitEvent args)
    {
        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        // Применяем множитель урона (Sanroo = 0.5, MalorianArms = 1.5)
        if (component.DamageMultiplier != 1f)
        {
            gun.DamageModifier *= component.DamageMultiplier;
        }

        // Применяем множитель скорострельности (drawTimeMultiplier → UseDelay)
        // UseDelay блокирует стрельбу на N секунд после экипировки оружия
        if (component.DrawTimeMultiplier > 1f)
        {
            var delay = EnsureComp<UseDelayComponent>(uid);
            var drawDelay = TimeSpan.FromSeconds(component.DrawTimeMultiplier);
            _useDelay.SetLength((uid, delay), drawDelay);
        }

        // Применяем множитель перезарядки к FillDelay на BallisticAmmoProvider
        if (component.ReloadTimeMultiplier != 1f && TryComp<BallisticAmmoProviderComponent>(uid, out var ballistic))
        {
            _sharedNcWeapon.SetBallisticFillDelay(uid, ballistic, TimeSpan.FromSeconds(ballistic.FillDelay.TotalSeconds * component.ReloadTimeMultiplier));
        }

        // Принудительно обновляем модификаторы (spread и др.)
        _gun.RefreshModifiers(uid);
    }

    /// <summary>
    /// Остывание оружия каждый тик.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NCWeaponComponent>();
        while (query.MoveNext(out var uid, out var weapon))
        {
            // Остывание оружия
            if (weapon.CurrentHeat > 0)
            {
                weapon.CurrentHeat = MathF.Max(0, weapon.CurrentHeat - weapon.CooldownRate * frameTime);

                // Сброс статуса перегрева
                if (weapon.IsOverheated && weapon.CurrentHeat < weapon.OverheatThreshold * 0.5f)
                {
                    weapon.IsOverheated = false;
                    Dirty(uid, weapon);
                }
            }
        }
    }

    #region Обработка выстрела

    /// <summary>
    /// Основной обработчик после выстрела. Применяет все механики бренда.
    /// </summary>
    private void OnGunShot(EntityUid uid, NCWeaponComponent component, ref GunShotEvent args)
    {
        var user = args.User;

        // 1. Деградация прочности
        ApplyDegradation(uid, component);

        // 2. Накопление тепла (для DarraPolytechnic и других)
        if (component.OverheatHeat > 0)
            ApplyHeat(uid, component, user);

        // 3. Проверка шанса заклинивания (GunMart и др.)
        CheckJam(uid, component, user);

        // 4. Проверка шанса взрыва (BudgetArms)
        if (component.ExplodeChance > 0)
            CheckExplode(uid, component, user);

        // 5. Проверка шанса безвозвратной поломки (DaiLung)
        if (component.BreakChance > 0)
            CheckBreak(uid, component, user);

        // 6. Урон от отдачи стрелку (MalorianArms, ConstitutionArms)
        if (component.RecoilDamagePenalty > 0)
            ApplyRecoilDamage(uid, component, user);

        // Увеличиваем счётчик выстрелов
        component.ShotsFired++;
        Dirty(uid, component);
    }

    #endregion

    #region Механики

    /// <summary>
    /// Деградация прочности за каждый выстрел.
    /// При достижении 0 — оружие ломается.
    /// </summary>
    private void ApplyDegradation(EntityUid uid, NCWeaponComponent component)
    {
        var oldDurability = component.Durability;
        component.Durability = MathF.Max(0, component.Durability - component.DegradationRate);

        // Рейзим событие деградации
        var ev = new WeaponDegradedEvent(uid, oldDurability, component.Durability);
        RaiseLocalEvent(uid, ref ev);

        // При нулевой прочности — поломка
        if (component.Durability <= 0 && !component.IsBroken)
        {
            component.IsBroken = true;

            var brokeEv = new WeaponBrokeEvent(default, uid);
            RaiseLocalEvent(uid, ref brokeEv);
        }
    }

    /// <summary>
    /// Накопление тепла при стрельбе.
    /// При перегреве — термический урон стрелку (DarraPolytechnic).
    /// </summary>
    private void ApplyHeat(EntityUid uid, NCWeaponComponent component, EntityUid user)
    {
        component.CurrentHeat += component.OverheatHeat;

        if (component.CurrentHeat >= component.OverheatThreshold)
        {
            component.IsOverheated = true;

            // Рейзим событие перегрева
            var ev = new WeaponOverheatEvent(user, uid, component.CurrentHeat);
            RaiseLocalEvent(uid, ref ev);

            // Наносим термический урон стрелку
            if (component.OverheatDamage != null)
            {
                _damageable.TryChangeDamage(user, component.OverheatDamage, origin: uid);

                // Звук ожога
                if (component.OverheatSound != null)
                    _audio.PlayPvs(component.OverheatSound, uid);

                _popup.PopupEntity(
                    Loc.GetString("nc-weapon-overheat", ("weapon", uid)),
                    uid,
                    user,
                    PopupType.LargeCaution);
            }
        }
    }

    /// <summary>
    /// Проверка шанса заклинивания.
    /// Шанс увеличивается при низкой прочности.
    /// </summary>
    private void CheckJam(EntityUid uid, NCWeaponComponent component, EntityUid user)
    {
        if (component.JamChance <= 0)
            return;

        // Прогрессивный шанс: чем ниже прочность, тем выше шанс заклинивания
        var durabilityRatio = component.MaxDurability > 0
            ? component.Durability / component.MaxDurability
            : 0f;
        var effectiveJamChance = MathF.Min(component.JamChance * (2f - durabilityRatio), 1f);

        if (!_random.Prob(effectiveJamChance))
            return;

        component.IsJammed = true;
        Dirty(uid, component);

        // Рейзим событие заклинивания
        var ev = new WeaponJammedEvent(user, uid);
        RaiseLocalEvent(uid, ref ev);

        _popup.PopupEntity(
            Loc.GetString("nc-weapon-jam", ("weapon", uid)),
            uid,
            user,
            PopupType.MediumCaution);
    }

    /// <summary>
    /// Проверка шанса взрыва оружия (BudgetArms).
    /// Шанс увеличивается при низкой прочности.
    /// </summary>
    private void CheckExplode(EntityUid uid, NCWeaponComponent component, EntityUid user)
    {
        // Шанс увеличивается при прочности ниже 30%
        var durabilityRatio = component.MaxDurability > 0
            ? component.Durability / component.MaxDurability
            : 1f;
        var effectiveChance = MathF.Min(
            durabilityRatio < 0.3f
                ? component.ExplodeChance * 3f
                : component.ExplodeChance,
            1f);

        if (!_random.Prob(effectiveChance))
            return;

        // Взрыв! Наносим урон стрелку
        if (component.ExplodeDamage != null)
            _damageable.TryChangeDamage(user, component.ExplodeDamage, origin: uid);

        // Звук взрыва
        if (component.ExplodeSound != null)
            _audio.PlayPvs(component.ExplodeSound, uid);

        // Рейзим событие взрыва
        var ev = new WeaponExplodedEvent(user, uid);
        RaiseLocalEvent(uid, ref ev);

        // Дропаем оружие из рук
        _hands.TryDrop(user, uid, checkActionBlocker: false);

        _popup.PopupEntity(
            Loc.GetString("nc-weapon-explode", ("weapon", uid)),
            uid,
            user,
            PopupType.LargeCaution);

        // Ломаем оружие
        component.IsBroken = true;
        component.IsRepairable = false; // BudgetArms — после взрыва не починить
        Dirty(uid, component);
    }

    /// <summary>
    /// Проверка безвозвратной поломки (DaiLung — «Китайская рулетка»).
    /// </summary>
    private void CheckBreak(EntityUid uid, NCWeaponComponent component, EntityUid user)
    {
        if (!_random.Prob(component.BreakChance))
            return;

        component.IsBroken = true;
        component.IsRepairable = false; // DaiLung — безвозвратная поломка
        Dirty(uid, component);

        var ev = new WeaponBrokeEvent(user, uid);
        RaiseLocalEvent(uid, ref ev);

        // Звук поломки
        if (component.BreakSound != null)
            _audio.PlayPvs(component.BreakSound, uid);

        _popup.PopupEntity(
            Loc.GetString("nc-weapon-break-permanent", ("weapon", uid)),
            uid,
            user,
            PopupType.LargeCaution);
    }

    /// <summary>
    /// Урон стрелку от отдачи (MalorianArms без кибер-рук, ConstitutionArms).
    /// </summary>
    private void ApplyRecoilDamage(EntityUid uid, NCWeaponComponent component, EntityUid user)
    {
        // TODO: Проверка на кибер-руки — пропускать урон если установлены
        // if (HasComp<CyberArmComponent>(user)) return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", component.RecoilDamagePenalty);

        _damageable.TryChangeDamage(user, damage, origin: uid);
    }

    #endregion

    #region Модификаторы

    /// <summary>
    /// Модификация параметров оружия при RefreshModifiers.
    /// Применяет множители разброса по бренду.
    /// </summary>
    private void OnRefreshModifiers(EntityUid uid, NCWeaponComponent component, ref GunRefreshModifiersEvent args)
    {
        // Множитель разброса (Sanroo = 2.5x)
        if (component.SpreadMultiplier != 1f)
        {
            args.AngleIncrease *= component.SpreadMultiplier;
            args.MaxAngle *= component.SpreadMultiplier;
        }

        // Штраф к точности при низкой прочности (для всех ТИР-0)
        if (component.Tier == WeaponTier.Poor && component.MaxDurability > 0)
        {
            var durabilityRatio = component.Durability / component.MaxDurability;
            if (durabilityRatio < 0.5f)
            {
                // Потеря точности при износе: до +50% разброса
                var penaltyMultiplier = 1f + (1f - durabilityRatio * 2f) * 0.5f;
                args.AngleIncrease *= penaltyMultiplier;
            }
        }
    }

    #endregion

    #region Публичные методы

    /// <summary>
    /// Попытка расклинить оружие (вызывается из интеракции).
    /// </summary>
    public bool TryUnjam(EntityUid uid, EntityUid user, NCWeaponComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.IsJammed)
            return false;

        // Rostovic: ремонт ударом с 90% шансом
        if (component.Manufacturer == WeaponManufacturer.Rostovic)
        {
            if (_random.Prob(0.9f))
            {
                component.IsJammed = false;
                Dirty(uid, component);

                _popup.PopupEntity(
                    Loc.GetString("nc-weapon-unjam-rostovic"),
                    uid,
                    user,
                    PopupType.Medium);
                return true;
            }

            _popup.PopupEntity(
                Loc.GetString("nc-weapon-unjam-rostovic-fail"),
                uid,
                user,
                PopupType.SmallCaution);
            return false;
        }

        // Стандартная расклинка
        component.IsJammed = false;
        Dirty(uid, component);

        _popup.PopupEntity(
            Loc.GetString("nc-weapon-unjammed"),
            uid,
            user,
            PopupType.Medium);
        return true;
    }

    #endregion
}
