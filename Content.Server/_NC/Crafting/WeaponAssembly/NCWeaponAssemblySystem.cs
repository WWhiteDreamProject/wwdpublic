using Content.Shared._NC.Crafting.WeaponAssembly;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Tools;
using Robust.Shared.Containers;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server._NC.Crafting.WeaponAssembly;

/// <summary>
/// Серверная система финальной сборки оружия (Этап 3).
/// Игрок кликает по Чертежу (Blueprint) либо деталью, либо инструментом,
/// согласно строгой последовательности шагов. При ошибке - фатальный взрыв с осколками.
/// </summary>
public sealed class NCWeaponAssemblySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NCBlueprintComponent, InteractUsingEvent>(OnInteractUsingBlueprint);
        SubscribeLocalEvent<NCBlueprintComponent, NCAssemblyDoAfterEvent>(OnAssemblyDoAfter);
    }

    private void OnInteractUsingBlueprint(EntityUid uid, NCBlueprintComponent blueprint, InteractUsingEvent args)
    {
        if (args.Handled || blueprint.CurrentStep >= blueprint.Steps.Count)
            return;

        // Игнорируем предметы, которые не являются ни деталью, ни инструментом
        bool isPart = TryComp<NCWeaponPartComponent>(args.Used, out var usedPart);
        bool isTool = TryComp<ToolComponent>(args.Used, out var usedTool);

        if (!isPart && !isTool)
            return; // Просто игнорируем

        args.Handled = true;

        var currentStepDef = blueprint.Steps[blueprint.CurrentStep];

        // Ожидается деталь
        if (currentStepDef.Part.HasValue || !string.IsNullOrEmpty(currentStepDef.RequiredPrototype))
        {
            bool match = true;

            // Проверяем тип, если он задан
            if (currentStepDef.Part.HasValue && (!isPart || usedPart!.PartType != currentStepDef.Part.Value))
                match = false;

            // Проверяем конкретный прототип, если он задан
            if (match && !string.IsNullOrEmpty(currentStepDef.RequiredPrototype))
            {
                var prototype = MetaData(args.Used).EntityPrototype?.ID;
                if (prototype != currentStepDef.RequiredPrototype)
                    match = false;
            }

            if (match)
            {
                StartAssemblyDoAfter(uid, blueprint, args.Used, args.User, currentStepDef);
            }
            else
            {
                FailAssembly(uid, args.Used, args.User, blueprint.GarbageEntityId);
            }
            return;
        }

        // Ожидается инструмент
        if (!string.IsNullOrEmpty(currentStepDef.ToolQuality))
        {
            if (isTool && _toolSystem.HasQuality(args.Used, currentStepDef.ToolQuality, usedTool))
            {
                StartAssemblyDoAfter(uid, blueprint, args.Used, args.User, currentStepDef);
            }
            else
            {
                FailAssembly(uid, args.Used, args.User, blueprint.GarbageEntityId);
            }
            return;
        }
    }

    private void StartAssemblyDoAfter(EntityUid blueprintUid, NCBlueprintComponent blueprint, EntityUid usedUid, EntityUid user, NCAssemblyStep stepDef)
    {
        // Проигрываем звук, если он есть
        if (stepDef.Sound != null)
        {
            _audio.PlayPvs(stepDef.Sound, blueprintUid);
        }

        var ev = new NCAssemblyDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(EntityManager, user, stepDef.DoAfterTime, ev, blueprintUid, target: blueprintUid, used: usedUid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnAssemblyDoAfter(EntityUid uid, NCBlueprintComponent blueprint, NCAssemblyDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Used == null || blueprint.CurrentStep >= blueprint.Steps.Count)
            return;

        args.Handled = true;

        var usedUid = args.Used.Value;

        // Раз мы уже здесь, значит предмет был правильным (мы проверяли до старта DoAfter).
        // Но надо понять, деталь это или инструмент
        bool isPart = HasComp<NCWeaponPartComponent>(usedUid);

        SucceedStep(uid, blueprint, usedUid, args.User, !isPart);
    }

    private void SucceedStep(EntityUid blueprintUid, NCBlueprintComponent blueprint, EntityUid usedUid, EntityUid user, bool isTool)
    {
        if (!isTool)
        {
            // Уничтожаем деталь, так как она встроилась в чертеж
            QueueDel(usedUid);
            _popup.PopupEntity("✔ Деталь установлена.", user);
        }
        else
        {
            _popup.PopupEntity("✔ Инструмент применён.", user);
        }

        blueprint.CurrentStep++;

        // Проверяем, завершена ли сборка
        if (blueprint.CurrentStep >= blueprint.Steps.Count)
        {
            _popup.PopupEntity("Сборка успешно завершена!", user);

            if (!string.IsNullOrEmpty(blueprint.ResultEntityId))
            {
                Spawn(blueprint.ResultEntityId, Transform(blueprintUid).Coordinates);
            }

            // Удаляем сам чертеж
            QueueDel(blueprintUid);
        }
    }

    private void FailAssembly(EntityUid blueprintUid, EntityUid usedUid, EntityUid user, string garbageId)
    {
        _popup.PopupEntity("☠ ФАТАЛЬНАЯ ОШИБКА СБОРКИ! Запчасти уничтожены!", user);

        var coords = Transform(user).Coordinates;

        // Уничтожаем предмет в руке ТОЛЬКО если это деталь (мы не уничтожаем отвертку игрока)
        if (HasComp<NCWeaponPartComponent>(usedUid))
        {
            QueueDel(usedUid);
        }

        // Взрыв Чертежа (Слабый взрыв: 30 интенсивность, 2 наклон, макс 5 = взрыв на ~2-3 тайла)
        _explosion.QueueExplosion(blueprintUid, ExplosionSystem.DefaultExplosionPrototypeId, 30, 2, 5);

        // Спавн "осколков" - разбрасываем мусор вокруг
        int shrapnelCount = _random.Next(3, 7);
        for (int i = 0; i < shrapnelCount; i++)
        {
            var offset = new Vector2(_random.NextFloat(-0.8f, 0.8f), _random.NextFloat(-0.8f, 0.8f));
            Spawn(garbageId, coords.Offset(offset));
        }

        // Удаляем чертеж
        QueueDel(blueprintUid);
    }
}
