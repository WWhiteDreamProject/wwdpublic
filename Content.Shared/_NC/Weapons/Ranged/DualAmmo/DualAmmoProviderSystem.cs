// Стукач Томми — система для двойного магазина (пистолет + дробовик)
// Путь: Content.Shared/_NC/Weapons/Ranged/DualAmmo/DualAmmoProviderSystem.cs

using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._NC.Weapons.Ranged.DualAmmo;

/// <summary>
/// Система для управления оружием с двумя независимыми магазинами.
/// Обеспечивает переключение между режимами огня и синхронизацию с BallisticAmmoProvider.
/// </summary>
public sealed class DualAmmoProviderSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Переключение режима через Alt+Click (AlternativeVerb)
        SubscribeLocalEvent<DualAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);

        // Меню выбора режима через verb
        SubscribeLocalEvent<DualAmmoProviderComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        // Отображение текущего режима при осмотре
        SubscribeLocalEvent<DualAmmoProviderComponent, ExaminedEvent>(OnExamine);

        // Инициализация при старте — синхронизация с BallisticAmmoProvider
        SubscribeLocalEvent<DualAmmoProviderComponent, ComponentStartup>(OnStartup);

        // Сохранение патронов при смене режима
        SubscribeLocalEvent<DualAmmoProviderComponent, GunShotEvent>(OnGunShot);
    }

    /// <summary>
    /// Инициализация компонента — устанавливаем начальный режим.
    /// </summary>
    private void OnStartup(EntityUid uid, DualAmmoProviderComponent component, ComponentStartup args)
    {
        if (component.Modes.Count == 0)
            return;

        // Применяем текущий режим к BallisticAmmoProvider
        ApplyMode(uid, component, component.CurrentMode, null, silent: true);
    }

    /// <summary>
    /// Добавление верба для альтернативного действия (Alt+Click).
    /// </summary>
    private void OnGetAlternativeVerbs(EntityUid uid, DualAmmoProviderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.Modes.Count < 2)
            return;

        var nextModeIndex = (component.CurrentMode + 1) % component.Modes.Count;
        var nextMode = component.Modes[nextModeIndex];
        var modeName = Loc.GetString(nextMode.ModeName);

        var verb = new AlternativeVerb
        {
            Act = () => CycleMode(uid, component, args.User),
            Text = Loc.GetString("dual-ammo-switch-mode", ("mode", modeName)),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Добавление verb для выбора конкретного режима.
    /// </summary>
    private void OnGetVerbs(EntityUid uid, DualAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.Modes.Count < 2)
            return;

        for (var i = 0; i < component.Modes.Count; i++)
        {
            var mode = component.Modes[i];
            var index = i;

            // Получаем название режима
            var modeName = Loc.GetString(mode.ModeName);

            var verb = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = modeName,
                Disabled = i == component.CurrentMode,
                Impact = Database.LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetMode(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(verb);
        }
    }

    /// <summary>
    /// Отображение текущего режима при осмотре оружия.
    /// </summary>
    private void OnExamine(EntityUid uid, DualAmmoProviderComponent component, ExaminedEvent args)
    {
        if (component.Modes.Count < 2)
            return;

        var currentMode = component.Modes[component.CurrentMode];
        var modeName = Loc.GetString(currentMode.ModeName);

        args.PushMarkup(Loc.GetString("dual-ammo-current-mode", ("mode", modeName)));
    }

    /// <summary>
    /// Синхронизация патронов после выстрела.
    /// </summary>
    private void OnGunShot(EntityUid uid, DualAmmoProviderComponent component, ref GunShotEvent args)
    {
        // Обновляем счётчик патронов в текущем режиме
        SyncAmmoFromProvider(uid, component);
    }

    /// <summary>
    /// Циклическое переключение на следующий режим.
    /// </summary>
    private void CycleMode(EntityUid uid, DualAmmoProviderComponent component, EntityUid user)
    {
        if (component.Modes.Count < 2)
            return;

        var nextMode = (component.CurrentMode + 1) % component.Modes.Count;
        SetMode(uid, component, nextMode, user);
    }

    /// <summary>
    /// Установка конкретного режима огня.
    /// </summary>
    public void SetMode(EntityUid uid, DualAmmoProviderComponent component, int modeIndex, EntityUid? user)
    {
        if (modeIndex < 0 || modeIndex >= component.Modes.Count)
            return;

        if (modeIndex == component.CurrentMode)
            return;

        // Сохраняем текущее количество патронов перед переключением
        SyncAmmoFromProvider(uid, component);

        // Применяем новый режим
        ApplyMode(uid, component, modeIndex, user);
    }

    /// <summary>
    /// Применение режима к BallisticAmmoProvider.
    /// </summary>
    private void ApplyMode(EntityUid uid, DualAmmoProviderComponent component, int modeIndex, EntityUid? user, bool silent = false)
    {
        var mode = component.Modes[modeIndex];

        // Обновляем текущий режим
        component.CurrentMode = modeIndex;
        Dirty(uid, component);

        if (mode.UsesMagazine)
        {
            // Переключаемся на магазинное питание
            EnsureComp<MagazineAmmoProviderComponent>(uid);
            RemComp<BallisticAmmoProviderComponent>(uid);
        }
        else
        {
            // Переключаемся на встроенное баллистическое питание
            var ballistic = EnsureComp<BallisticAmmoProviderComponent>(uid);
            RemComp<MagazineAmmoProviderComponent>(uid);

            ballistic.Proto = mode.Prototype;
            ballistic.Capacity = mode.Capacity;
            ballistic.UnspawnedCount = mode.Count;

            if (mode.WhitelistTags != null)
            {
                var whitelist = new EntityWhitelist();
                whitelist.Tags = new List<ProtoId<TagPrototype>>();
                foreach (var tag in mode.WhitelistTags)
                {
                    whitelist.Tags.Add(tag);
                }
                ballistic.Whitelist = whitelist;
            }
        }

        // Обновляем параметры Gun если указаны
        if (TryComp<GunComponent>(uid, out var gun))
        {
            if (mode.FireRate.HasValue)
                gun.FireRate = mode.FireRate.Value;

            if (mode.SoundGunshot is not null)
                gun.SoundGunshot = mode.SoundGunshot;

            // Обновляем модификаторы
            _gun.RefreshModifiers(uid);
        }

        // Показываем popup и проигрываем звук (только для первого предсказания)
        if (!silent && user.HasValue && _timing.IsFirstTimePredicted)
        {
            var modeName = Loc.GetString(mode.ModeName);
            _popup.PopupEntity(Loc.GetString("dual-ammo-switch-mode", ("mode", modeName)), uid, user.Value);

            // Звук переключения режима
            _audio.PlayPredicted(gun?.SoundMode, uid, user);
        }
    }

    /// <summary>
    /// Синхронизация количества патронов из BallisticAmmoProvider в текущий режим.
    /// Эта функция также ОЧИЩАЕТ контейнер, переводя все патроны в счетчик.
    /// Это необходимо, чтобы при смене типа боеприпасов в контейнере не оставались несовместимые патроны.
    /// </summary>
    private void SyncAmmoFromProvider(EntityUid uid, DualAmmoProviderComponent component)
    {
        if (component.CurrentMode < 0 || component.CurrentMode >= component.Modes.Count)
            return;

        var mode = component.Modes[component.CurrentMode];

        // Если режим использует внешний магазин, нам не нужно синхронизировать внутренний счётчик патронов
        if (mode.UsesMagazine)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(uid, out var ballistic))
            return;

        // Считаем общее количество патронов (виртуальные + реальные)
        var totalCount = ballistic.Count;

        // Сохраняем в текущий режим
        component.Modes[component.CurrentMode].Count = totalCount;

        // Очищаем контейнер от реальных сущностей, так как мы их "виртуализировали"
        if (ballistic.Container != null)
        {
            var entities = ballistic.Container.ContainedEntities.ToList();
            foreach (var ent in entities)
            {
                // Удаляем сущности патронов
                QueueDel(ent);
            }
            _container.EmptyContainer(ballistic.Container);
        }

        // Очищаем список сущностей в компоненте, чтобы избежать доступа к удаленным объектам
        ballistic.Entities.Clear();

        // Обновляем UnspawnedCount на всякий случай, хотя при смене режима он перезапишется
        ballistic.UnspawnedCount = totalCount;

        Dirty(uid, component);
    }
}
